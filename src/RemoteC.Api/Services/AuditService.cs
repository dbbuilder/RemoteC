using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RemoteC.Data;
using RemoteC.Data.Entities;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Services
{
    /// <summary>
    /// Service for comprehensive audit logging
    /// </summary>
    public class AuditService : IAuditService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDistributedCache _cache;
        private readonly ILogger<AuditService> _logger;
        private readonly AuditOptions _options;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly SemaphoreSlim _batchSemaphore;
        private readonly List<AuditLogEntry> _batchBuffer;
        private readonly Timer? _batchTimer;

        public AuditService(
            IServiceProvider serviceProvider,
            IDistributedCache cache,
            ILogger<AuditService> logger,
            IOptions<AuditOptions> options,
            IBackgroundTaskQueue taskQueue)
        {
            _serviceProvider = serviceProvider;
            _cache = cache;
            _logger = logger;
            _options = options.Value;
            _taskQueue = taskQueue;
            _batchSemaphore = new SemaphoreSlim(1, 1);
            _batchBuffer = new List<AuditLogEntry>();
            
            // Start batch timer if enabled
            if (_options.EnableBatching)
            {
                _batchTimer = new Timer(
                    async _ => await FlushBatchAsync(),
                    null,
                    TimeSpan.FromSeconds(_options.BatchIntervalSeconds),
                    TimeSpan.FromSeconds(_options.BatchIntervalSeconds));
            }
        }

        /// <summary>
        /// Log an audit entry
        /// </summary>
        public async Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate entry
                ValidateEntry(entry);

                // Apply filters
                if (!ShouldLog(entry))
                {
                    return;
                }

                // Check if batching is enabled
                if (_options.EnableBatching)
                {
                    await AddToBatchAsync(entry);
                }
                else
                {
                    await LogDirectlyAsync(entry, cancellationToken);
                }

                // Check for alerts
                await CheckForAlertsAsync(entry, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging audit entry");
                // Don't throw - audit logging should not break the application
            }
        }

        /// <summary>
        /// Log multiple audit entries in batch
        /// </summary>
        public async Task LogBatchAsync(IEnumerable<AuditLogEntry> entries, CancellationToken cancellationToken = default)
        {
            var validEntries = entries.Where(e => 
            {
                try
                {
                    ValidateEntry(e);
                    return ShouldLog(e);
                }
                catch
                {
                    return false;
                }
            }).ToList();

            if (!validEntries.Any()) return;

            try
            {
                // Convert to entities
                var auditLogs = validEntries.Select(e => ConvertToEntity(e)).ToList();

                // Use a new service scope for batch operations
                using var scope = _serviceProvider.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<RemoteCDbContext>();
                
                // Batch insert
                await context.AuditLogs.AddRangeAsync(auditLogs, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                // Check for alerts
                foreach (var entry in validEntries)
                {
                    await CheckForAlertsAsync(entry, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging batch audit entries");
            }
        }

        /// <summary>
        /// Query audit logs with filtering
        /// </summary>
        public async Task<AuditLogQueryResult> QueryAsync(AuditLogQuery query, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<RemoteCDbContext>();
            var queryable = context.AuditLogs.AsQueryable();

            // Apply filters
            if (query.OrganizationId.HasValue)
                queryable = queryable.Where(a => a.OrganizationId == query.OrganizationId.Value);

            if (query.UserId.HasValue)
                queryable = queryable.Where(a => a.UserId == query.UserId.Value);

            if (query.StartDate.HasValue)
                queryable = queryable.Where(a => a.Timestamp >= query.StartDate.Value);

            if (query.EndDate.HasValue)
                queryable = queryable.Where(a => a.Timestamp <= query.EndDate.Value);

            if (!string.IsNullOrEmpty(query.Action))
                queryable = queryable.Where(a => a.Action == query.Action);

            if (!string.IsNullOrEmpty(query.ResourceType))
                queryable = queryable.Where(a => a.ResourceType == query.ResourceType);

            if (!string.IsNullOrEmpty(query.ResourceId))
                queryable = queryable.Where(a => a.ResourceId == query.ResourceId);

            if (query.MinSeverity.HasValue)
                queryable = queryable.Where(a => (int)a.Severity >= (int)query.MinSeverity.Value);

            if (query.Category.HasValue)
                queryable = queryable.Where(a => (int)a.Category == (int)query.Category.Value);

            if (!string.IsNullOrEmpty(query.SearchText))
            {
                queryable = queryable.Where(a => 
                    (a.Details != null && a.Details.Contains(query.SearchText)) ||
                    (a.UserName != null && a.UserName.Contains(query.SearchText)) ||
                    (a.UserEmail != null && a.UserEmail.Contains(query.SearchText)) ||
                    (a.ResourceName != null && a.ResourceName.Contains(query.SearchText)));
            }

            if (!string.IsNullOrEmpty(query.IpAddress))
                queryable = queryable.Where(a => a.IpAddress == query.IpAddress);

            if (query.SuccessOnly.HasValue)
                queryable = queryable.Where(a => a.Success == query.SuccessOnly.Value);

            // Get total count
            var totalCount = await queryable.CountAsync(cancellationToken);

            // Apply sorting
            queryable = ApplySorting(queryable, query.SortBy, query.SortDescending);

            // Apply pagination
            var items = await queryable
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(a => ConvertToModel(a))
                .ToListAsync(cancellationToken);

            return new AuditLogQueryResult
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
        }

        /// <summary>
        /// Get audit logs for a specific resource
        /// </summary>
        public async Task<List<AuditLogEntry>> GetByResourceAsync(
            string resourceType, 
            string resourceId, 
            int limit = 100,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"audit:resource:{resourceType}:{resourceId}";
            var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
            
            if (!string.IsNullOrEmpty(cached))
            {
                return JsonSerializer.Deserialize<List<AuditLogEntry>>(cached) ?? new List<AuditLogEntry>();
            }

            using var scope = _serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<RemoteCDbContext>();
            var logs = await context.AuditLogs
                .Where(a => a.ResourceType == resourceType && a.ResourceId == resourceId)
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .Select(a => ConvertToModel(a))
                .ToListAsync(cancellationToken);

            // Cache for 5 minutes
            await _cache.SetStringAsync(
                cacheKey, 
                JsonSerializer.Serialize(logs),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                },
                cancellationToken);

            return logs;
        }

        /// <summary>
        /// Get audit logs for a specific user
        /// </summary>
        public async Task<List<AuditLogEntry>> GetByUserAsync(
            Guid userId, 
            DateTime? startDate = null, 
            DateTime? endDate = null,
            int limit = 100,
            CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<RemoteCDbContext>();
            var query = context.AuditLogs
                .Where(a => a.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(a => a.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.Timestamp <= endDate.Value);

            return await query
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .Select(a => ConvertToModel(a))
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Export audit logs
        /// </summary>
        public async Task<byte[]> ExportAsync(
            AuditLogExportOptions options,
            CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<RemoteCDbContext>();
            var query = context.AuditLogs
                .Where(a => a.OrganizationId == options.OrganizationId &&
                           a.Timestamp >= options.StartDate &&
                           a.Timestamp <= options.EndDate);

            if (options.Categories?.Any() == true)
                query = query.Where(a => options.Categories.Cast<int>().Contains((int)a.Category));

            if (options.Actions?.Any() == true)
                query = query.Where(a => options.Actions.Contains(a.Action));

            var logs = await query
                .OrderBy(a => a.Timestamp)
                .Select(a => ConvertToModel(a))
                .ToListAsync(cancellationToken);

            return options.Format switch
            {
                ExportFormat.Json => ExportToJson(logs, options),
                ExportFormat.Csv => ExportToCsv(logs, options),
                ExportFormat.Xml => ExportToXml(logs, options),
                ExportFormat.Pdf => await ExportToPdfAsync(logs, options),
                _ => throw new NotSupportedException($"Export format {options.Format} is not supported")
            };
        }

        /// <summary>
        /// Delete old audit logs based on retention policy
        /// </summary>
        public async Task<int> DeleteOldLogsAsync(
            DateTime cutoffDate,
            CancellationToken cancellationToken = default)
        {
            // Archive before deleting if configured
            if (_options.EnableArchiving)
            {
                await ArchiveOldLogsAsync(cutoffDate, cancellationToken);
            }

            // Delete in batches to avoid timeout
            var totalDeleted = 0;
            const int batchSize = 1000;

            while (true)
            {
                using var scope = _serviceProvider.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<RemoteCDbContext>();
                var toDelete = await context.AuditLogs
                    .Where(a => a.Timestamp < cutoffDate)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                if (!toDelete.Any()) break;

                context.AuditLogs.RemoveRange(toDelete);
                var deleted = await context.SaveChangesAsync(cancellationToken);
                totalDeleted += deleted;

                _logger.LogInformation("Deleted {Count} old audit logs", deleted);
            }

            return totalDeleted;
        }

        /// <summary>
        /// Get audit statistics
        /// </summary>
        public async Task<AuditStatistics> GetStatisticsAsync(
            Guid organizationId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"audit:stats:{organizationId}:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
            var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
            
            if (!string.IsNullOrEmpty(cached))
            {
                return JsonSerializer.Deserialize<AuditStatistics>(cached) ?? new AuditStatistics();
            }

            using var scope = _serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<RemoteCDbContext>();
            var logs = await context.AuditLogs
                .Where(a => a.OrganizationId == organizationId &&
                           a.Timestamp >= startDate &&
                           a.Timestamp <= endDate)
                .ToListAsync(cancellationToken);

            var stats = new AuditStatistics
            {
                TotalEvents = logs.Count,
                EventsByAction = logs.GroupBy(a => a.Action)
                    .ToDictionary(g => g.Key, g => g.Count()),
                EventsByCategory = logs.GroupBy(a => (AuditCategory)a.Category)
                    .ToDictionary(g => g.Key, g => g.Count()),
                EventsBySeverity = logs.GroupBy(a => (AuditSeverity)a.Severity)
                    .ToDictionary(g => g.Key, g => g.Count()),
                EventsByUser = logs.Where(a => a.UserName != null)
                    .GroupBy(a => a.UserName!)
                    .ToDictionary(g => g.Key, g => g.Count())
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(10)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                EventsByDay = logs.GroupBy(a => a.Timestamp.Date)
                    .ToDictionary(g => g.Key, g => g.Count()),
                FailedEvents = logs.Count(a => !a.Success),
                AverageResponseTime = logs.Where(a => a.Duration.HasValue)
                    .Select(a => a.Duration!.Value.TotalMilliseconds)
                    .DefaultIfEmpty(0)
                    .Average(),
                TopUsers = logs.Where(a => a.UserName != null)
                    .GroupBy(a => a.UserName!)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => g.Key)
                    .ToList(),
                TopActions = logs.GroupBy(a => a.Action)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => g.Key)
                    .ToList(),
                RecentAlerts = await GetRecentAlertsAsync(organizationId, cancellationToken)
            };

            // Cache for 15 minutes
            await _cache.SetStringAsync(
                cacheKey, 
                JsonSerializer.Serialize(stats),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                },
                cancellationToken);

            return stats;
        }

        #region Private Methods

        private void ValidateEntry(AuditLogEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            if (string.IsNullOrEmpty(entry.Action))
                throw new ArgumentException("Action is required", nameof(entry));

            if (string.IsNullOrEmpty(entry.ResourceType))
                throw new ArgumentException("ResourceType is required", nameof(entry));

            // OrganizationId is required, but allow default organization (all zeros) for development scenarios
            // No additional validation needed since Guid.Empty is now acceptable for default organization
        }

        private bool ShouldLog(AuditLogEntry entry)
        {
            // Check if action is in exclude list
            if (_options.ExcludedActions?.Contains(entry.Action) == true)
                return false;

            // Check if severity meets minimum threshold
            if (entry.Severity < _options.MinimumSeverity)
                return false;

            // Check if category is enabled
            if (_options.EnabledCategories?.Any() == true && 
                !_options.EnabledCategories.Contains(entry.Category))
                return false;

            return true;
        }

        private async Task AddToBatchAsync(AuditLogEntry entry)
        {
            await _batchSemaphore.WaitAsync();
            try
            {
                _batchBuffer.Add(entry);

                if (_batchBuffer.Count >= _options.BatchSize)
                {
                    await FlushBatchAsync();
                }
            }
            finally
            {
                _batchSemaphore.Release();
            }
        }

        private async Task FlushBatchAsync()
        {
            await _batchSemaphore.WaitAsync();
            try
            {
                if (!_batchBuffer.Any()) return;

                var toFlush = _batchBuffer.ToList();
                _batchBuffer.Clear();

                // Queue background task
                await _taskQueue.QueueBackgroundWorkItemAsync(async token =>
                {
                    await LogBatchAsync(toFlush, token);
                });
            }
            finally
            {
                _batchSemaphore.Release();
            }
        }

        private async Task LogDirectlyAsync(AuditLogEntry entry, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<RemoteCDbContext>();
            var entity = ConvertToEntity(entry);
            context.AuditLogs.Add(entity);
            await context.SaveChangesAsync(cancellationToken);
        }

        private async Task CheckForAlertsAsync(AuditLogEntry entry, CancellationToken cancellationToken)
        {
            // Check for security alerts
            if (entry.Category == AuditCategory.Security && entry.Severity >= AuditSeverity.Warning)
            {
                await RaiseSecurityAlertAsync(entry, cancellationToken);
            }

            // Check for repeated failures
            if (!entry.Success)
            {
                await CheckRepeatedFailuresAsync(entry, cancellationToken);
            }

            // Check for suspicious patterns
            await CheckSuspiciousPatternsAsync(entry, cancellationToken);
        }

        private async Task RaiseSecurityAlertAsync(AuditLogEntry entry, CancellationToken cancellationToken)
        {
            var alert = new AuditAlert
            {
                Type = "SecurityAlert",
                Description = $"Security event: {entry.Action} on {entry.ResourceType}",
                Timestamp = entry.Timestamp,
                Severity = entry.Severity,
                Count = 1
            };

            // Store alert
            var alertKey = $"audit:alert:security:{entry.OrganizationId}";
            await _cache.SetStringAsync(
                alertKey,
                JsonSerializer.Serialize(alert),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                },
                cancellationToken);

            _logger.LogWarning("Security alert raised: {Action} by {User}", entry.Action, entry.UserName);
        }

        private async Task CheckRepeatedFailuresAsync(AuditLogEntry entry, CancellationToken cancellationToken)
        {
            var failureKey = $"audit:failures:{entry.UserId}:{entry.Action}";
            var failures = await _cache.GetStringAsync(failureKey, cancellationToken);
            var failureCount = string.IsNullOrEmpty(failures) ? 0 : int.Parse(failures);
            
            failureCount++;
            
            if (failureCount >= _options.FailureAlertThreshold)
            {
                var alert = new AuditAlert
                {
                    Type = "RepeatedFailure",
                    Description = $"Repeated failures for {entry.Action} by {entry.UserName}",
                    Timestamp = DateTime.UtcNow,
                    Severity = AuditSeverity.Warning,
                    Count = failureCount
                };

                _logger.LogWarning("Repeated failure alert: {Count} failures for {Action} by {User}", 
                    failureCount, entry.Action, entry.UserName);
            }

            await _cache.SetStringAsync(
                failureKey,
                failureCount.ToString(),
                new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(15)
                },
                cancellationToken);
        }

        private async Task CheckSuspiciousPatternsAsync(AuditLogEntry entry, CancellationToken cancellationToken)
        {
            // Check for unusual access times
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(entry.Timestamp, TimeZoneInfo.Local);
            if (localTime.Hour < 6 || localTime.Hour > 22)
            {
                _logger.LogInformation("Unusual access time detected: {Action} at {Time} by {User}",
                    entry.Action, localTime, entry.UserName);
            }

            // Check for rapid actions
            var rapidKey = $"audit:rapid:{entry.UserId}";
            var lastAction = await _cache.GetStringAsync(rapidKey, cancellationToken);
            
            if (!string.IsNullOrEmpty(lastAction))
            {
                var lastTime = DateTime.Parse(lastAction);
                if ((entry.Timestamp - lastTime).TotalSeconds < 1)
                {
                    _logger.LogWarning("Rapid actions detected for user {User}", entry.UserName);
                }
            }

            await _cache.SetStringAsync(
                rapidKey,
                entry.Timestamp.ToString("O"),
                new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(1)
                },
                cancellationToken);
        }

        private async Task<List<AuditAlert>> GetRecentAlertsAsync(
            Guid organizationId, 
            CancellationToken cancellationToken)
        {
            // Get alerts from cache
            var alerts = new List<AuditAlert>();
            
            // Security alerts
            var securityKey = $"audit:alert:security:{organizationId}";
            var securityAlert = await _cache.GetStringAsync(securityKey, cancellationToken);
            if (!string.IsNullOrEmpty(securityAlert))
            {
                var alert = JsonSerializer.Deserialize<AuditAlert>(securityAlert);
                if (alert != null) alerts.Add(alert);
            }

            return alerts.OrderByDescending(a => a.Timestamp).Take(10).ToList();
        }

        private IQueryable<AuditLog> ApplySorting(IQueryable<AuditLog> query, string sortBy, bool descending)
        {
            return sortBy?.ToLower() switch
            {
                "timestamp" => descending ? query.OrderByDescending(a => a.Timestamp) : query.OrderBy(a => a.Timestamp),
                "user" => descending ? query.OrderByDescending(a => a.UserName) : query.OrderBy(a => a.UserName),
                "action" => descending ? query.OrderByDescending(a => a.Action) : query.OrderBy(a => a.Action),
                "severity" => descending ? query.OrderByDescending(a => a.Severity) : query.OrderBy(a => a.Severity),
                _ => descending ? query.OrderByDescending(a => a.Timestamp) : query.OrderBy(a => a.Timestamp)
            };
        }

        private AuditLog ConvertToEntity(AuditLogEntry entry)
        {
            return new AuditLog
            {
                Id = entry.Id,
                Timestamp = entry.Timestamp,
                OrganizationId = entry.OrganizationId,
                UserId = entry.UserId,
                UserName = entry.UserName,
                UserEmail = entry.UserEmail,
                IpAddress = entry.IpAddress,
                UserAgent = entry.UserAgent,
                Action = entry.Action,
                ResourceType = entry.ResourceType,
                ResourceId = entry.ResourceId,
                ResourceName = entry.ResourceName,
                Severity = (int)entry.Severity,
                Category = (int)entry.Category,
                Details = entry.Details,
                Metadata = entry.Metadata != null ? JsonSerializer.Serialize(entry.Metadata) : null,
                CorrelationId = entry.CorrelationId,
                Duration = entry.Duration,
                Success = entry.Success,
                ErrorMessage = entry.ErrorMessage,
                StackTrace = entry.StackTrace
            };
        }

        private AuditLogEntry ConvertToModel(AuditLog entity)
        {
            return new AuditLogEntry
            {
                Id = entity.Id,
                Timestamp = entity.Timestamp,
                OrganizationId = entity.OrganizationId ?? Guid.Empty,
                UserId = entity.UserId,
                UserName = entity.UserName,
                UserEmail = entity.UserEmail,
                IpAddress = entity.IpAddress,
                UserAgent = entity.UserAgent,
                Action = entity.Action,
                ResourceType = entity.ResourceType,
                ResourceId = entity.ResourceId,
                ResourceName = entity.ResourceName,
                Severity = (RemoteC.Shared.Models.AuditSeverity)entity.Severity,
                Category = (RemoteC.Shared.Models.AuditCategory)entity.Category,
                Details = entity.Details,
                Metadata = !string.IsNullOrEmpty(entity.Metadata) 
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(entity.Metadata) 
                    : null,
                CorrelationId = entity.CorrelationId,
                Duration = entity.Duration,
                Success = entity.Success,
                ErrorMessage = entity.ErrorMessage,
                StackTrace = entity.StackTrace
            };
        }

        private byte[] ExportToJson(List<AuditLogEntry> logs, AuditLogExportOptions options)
        {
            var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            return Encoding.UTF8.GetBytes(json);
        }

        private byte[] ExportToCsv(List<AuditLogEntry> logs, AuditLogExportOptions options)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Timestamp,Organization,User,Action,ResourceType,ResourceId,Severity,Category,Success,Details");
            
            foreach (var log in logs)
            {
                csv.AppendLine($"{log.Timestamp:yyyy-MM-dd HH:mm:ss},{log.OrganizationId},{log.UserName},{log.Action},{log.ResourceType},{log.ResourceId},{log.Severity},{log.Category},{log.Success},\"{log.Details}\"");
            }
            
            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        private byte[] ExportToXml(List<AuditLogEntry> logs, AuditLogExportOptions options)
        {
            var xml = new XElement("AuditLogs",
                new XAttribute("ExportDate", DateTime.UtcNow),
                new XAttribute("Organization", options.OrganizationId),
                logs.Select(log => new XElement("AuditLog",
                    new XElement("Timestamp", log.Timestamp),
                    new XElement("User", log.UserName),
                    new XElement("Action", log.Action),
                    new XElement("ResourceType", log.ResourceType),
                    new XElement("ResourceId", log.ResourceId),
                    new XElement("Severity", log.Severity),
                    new XElement("Category", log.Category),
                    new XElement("Success", log.Success),
                    new XElement("Details", log.Details)
                ))
            );
            
            return Encoding.UTF8.GetBytes(xml.ToString());
        }

        private async Task<byte[]> ExportToPdfAsync(List<AuditLogEntry> logs, AuditLogExportOptions options)
        {
            // Placeholder implementation - in production would use a PDF library like iTextSharp or QuestPDF
            await Task.CompletedTask;
            
            var pdfContent = new System.Text.StringBuilder();
            pdfContent.AppendLine("AUDIT LOG REPORT");
            pdfContent.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            pdfContent.AppendLine($"Total Entries: {logs.Count}");
            pdfContent.AppendLine(new string('-', 80));
            
            foreach (var log in logs)
            {
                pdfContent.AppendLine($"Timestamp: {log.Timestamp:yyyy-MM-dd HH:mm:ss}");
                pdfContent.AppendLine($"User: {log.UserName} ({log.UserId})");
                pdfContent.AppendLine($"Action: {log.Action}");
                pdfContent.AppendLine($"Resource: {log.ResourceType} - {log.ResourceId}");
                pdfContent.AppendLine($"Details: {log.Details}");
                pdfContent.AppendLine(new string('-', 40));
            }
            
            // Return as UTF-8 bytes (in production would be actual PDF bytes)
            return System.Text.Encoding.UTF8.GetBytes(pdfContent.ToString());
        }

        private async Task ArchiveOldLogsAsync(DateTime cutoffDate, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<RemoteCDbContext>();
                // Get logs older than cutoff date
                var logsToArchive = await context.AuditLogs
                    .Where(al => al.Timestamp < cutoffDate)
                    .ToListAsync(cancellationToken);
                
                if (!logsToArchive.Any())
                {
                    _logger.LogInformation("No audit logs found older than {CutoffDate}", cutoffDate);
                    return;
                }
                
                _logger.LogInformation("Archiving {Count} audit logs older than {CutoffDate}", 
                    logsToArchive.Count, cutoffDate);
                
                // In production, would:
                // 1. Serialize logs to compressed format
                // 2. Upload to cold storage (Azure Blob Storage Archive tier)
                // 3. Verify upload success
                // 4. Delete from primary database
                
                // For now, just mark as archived
                foreach (var log in logsToArchive)
                {
                    log.IsArchived = true;
                    log.ArchivedAt = DateTime.UtcNow;
                }
                
                await context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully archived {Count} audit logs", logsToArchive.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving audit logs older than {CutoffDate}", cutoffDate);
                throw;
            }
        }

        #endregion

        public async Task LogActionAsync(string action, string entityType, string entityId, 
            object? oldValue = null, object? newValue = null, object? metadata = null)
        {
            var entry = new AuditLogEntry
            {
                Action = action,
                ResourceType = entityType,
                ResourceId = entityId,
                Timestamp = DateTime.UtcNow,
                Category = DetermineCategory(action),
                Severity = AuditSeverity.Info,
                Success = true
            };

            if (metadata != null)
            {
                entry.Metadata = metadata as Dictionary<string, object> ?? 
                    new Dictionary<string, object> { ["data"] = metadata };
            }

            if (oldValue != null || newValue != null)
            {
                entry.Details = JsonSerializer.Serialize(new { oldValue, newValue });
            }

            await LogAsync(entry);
        }

        private AuditCategory DetermineCategory(string action)
        {
            if (action.Contains("login") || action.Contains("logout") || action.Contains("auth"))
                return AuditCategory.Authentication;
            if (action.Contains("permission") || action.Contains("role") || action.Contains("access"))
                return AuditCategory.Authorization;
            if (action.Contains("create") || action.Contains("update") || action.Contains("delete"))
                return AuditCategory.DataModification;
            if (action.Contains("read") || action.Contains("get") || action.Contains("view"))
                return AuditCategory.DataAccess;
            if (action.Contains("config") || action.Contains("setting"))
                return AuditCategory.Configuration;
            if (action.Contains("security") || action.Contains("encrypt"))
                return AuditCategory.Security;
            if (action.Contains("compliance") || action.Contains("audit"))
                return AuditCategory.Compliance;
            
            return AuditCategory.General;
        }

        public void Dispose()
        {
            _batchTimer?.Dispose();
            _batchSemaphore?.Dispose();
        }
    }

    public class AuditOptions
    {
        public bool EnableBatching { get; set; } = true;
        public int BatchSize { get; set; } = 100;
        public int BatchIntervalSeconds { get; set; } = 5;
        public bool EnableArchiving { get; set; } = true;
        public int RetentionDays { get; set; } = 365;
        public AuditSeverity MinimumSeverity { get; set; } = AuditSeverity.Info;
        public List<string>? ExcludedActions { get; set; }
        public List<AuditCategory>? EnabledCategories { get; set; }
        public int FailureAlertThreshold { get; set; } = 5;
        public string? ArchiveStorageConnectionString { get; set; }
    }

    public interface IBackgroundTaskQueue
    {
        Task QueueBackgroundWorkItemAsync(Func<CancellationToken, Task> workItem);
        Task<Func<CancellationToken, Task>?> DequeueAsync(CancellationToken cancellationToken);
    }
}