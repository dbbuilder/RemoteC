using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RemoteC.Api.Services;

namespace RemoteC.Api.Middleware
{
    /// <summary>
    /// Middleware for automatic audit logging of HTTP requests
    /// </summary>
    public class AuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditMiddleware> _logger;
        private readonly AuditMiddlewareOptions _options;

        public AuditMiddleware(
            RequestDelegate next,
            ILogger<AuditMiddleware> logger,
            AuditMiddlewareOptions options)
        {
            _next = next;
            _logger = logger;
            _options = options;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip if path is excluded
            if (_options.ExcludedPaths?.Any(path => 
                context.Request.Path.StartsWithSegments(path, StringComparison.OrdinalIgnoreCase)) == true)
            {
                await _next(context);
                return;
            }

            // Skip health checks and metrics
            if (context.Request.Path.StartsWithSegments("/health") ||
                context.Request.Path.StartsWithSegments("/metrics"))
            {
                await _next(context);
                return;
            }

            var auditService = context.RequestServices.GetService<IAuditService>();
            if (auditService == null)
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var correlationId = GetOrCreateCorrelationId(context);
            
            // Capture request body if enabled
            string? requestBody = null;
            if (_options.CaptureRequestBody && context.Request.ContentLength > 0)
            {
                requestBody = await CaptureRequestBodyAsync(context.Request);
            }

            // Store original response body stream
            var originalBodyStream = context.Response.Body;
            string? responseBody = null;

            try
            {
                // Replace response body stream to capture response
                using var responseBodyStream = new MemoryStream();
                context.Response.Body = responseBodyStream;

                // Process request
                await _next(context);

                // Capture response body if enabled
                if (_options.CaptureResponseBody && context.Response.ContentLength > 0)
                {
                    responseBody = await CaptureResponseBodyAsync(context.Response, responseBodyStream);
                }

                // Copy response to original stream
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalBodyStream);

                stopwatch.Stop();

                // Log successful request
                await LogAuditEntryAsync(
                    context, 
                    auditService, 
                    correlationId, 
                    stopwatch.Elapsed, 
                    requestBody, 
                    responseBody, 
                    true, 
                    null);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Log failed request
                await LogAuditEntryAsync(
                    context, 
                    auditService, 
                    correlationId, 
                    stopwatch.Elapsed, 
                    requestBody, 
                    null, 
                    false, 
                    ex);

                throw;
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        private async Task LogAuditEntryAsync(
            HttpContext context,
            IAuditService auditService,
            string correlationId,
            TimeSpan duration,
            string? requestBody,
            string? responseBody,
            bool success,
            Exception? exception)
        {
            try
            {
                var user = context.User;
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = user.FindFirst(ClaimTypes.Name)?.Value;
                var userEmail = user.FindFirst(ClaimTypes.Email)?.Value;
                var organizationId = user.FindFirst("OrganizationId")?.Value;

                // Determine action and resource from route
                var (action, resourceType, resourceId) = ExtractRouteInfo(context);

                var entry = new AuditLogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    OrganizationId = string.IsNullOrEmpty(organizationId) 
                        ? Guid.Empty 
                        : Guid.Parse(organizationId),
                    UserId = string.IsNullOrEmpty(userId) ? null : Guid.Parse(userId),
                    UserName = userName,
                    UserEmail = userEmail,
                    IpAddress = GetClientIpAddress(context),
                    UserAgent = context.Request.Headers["User-Agent"].FirstOrDefault(),
                    Action = action,
                    ResourceType = resourceType,
                    ResourceId = resourceId,
                    Severity = success ? AuditSeverity.Info : AuditSeverity.Error,
                    Category = DetermineCategory(context),
                    Details = BuildDetails(context, requestBody, responseBody),
                    Metadata = BuildMetadata(context),
                    CorrelationId = correlationId,
                    Duration = duration,
                    Success = success,
                    ErrorMessage = exception?.Message,
                    StackTrace = exception?.StackTrace
                };

                await auditService.LogAsync(entry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log audit entry");
            }
        }

        private string GetOrCreateCorrelationId(HttpContext context)
        {
            const string correlationIdHeader = "X-Correlation-Id";
            
            if (context.Request.Headers.TryGetValue(correlationIdHeader, out var correlationId))
            {
                return correlationId.ToString();
            }

            var newCorrelationId = Guid.NewGuid().ToString();
            context.Response.Headers[correlationIdHeader] = newCorrelationId;
            return newCorrelationId;
        }

        private string GetClientIpAddress(HttpContext context)
        {
            // Check for forwarded IP first (when behind proxy/load balancer)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private (string action, string resourceType, string? resourceId) ExtractRouteInfo(HttpContext context)
        {
            var method = context.Request.Method;
            var path = context.Request.Path.Value ?? "";
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == 0)
            {
                return ($"{method} /", "Root", null);
            }

            // Try to extract controller and action
            string resourceType = "Unknown";
            string? resourceId = null;
            string action = method;

            if (segments.Length >= 2 && segments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
            {
                resourceType = segments[1];
                
                if (segments.Length >= 3)
                {
                    // Check if third segment is a GUID
                    if (Guid.TryParse(segments[2], out _))
                    {
                        resourceId = segments[2];
                        action = segments.Length >= 4 ? $"{method} {segments[3]}" : method;
                    }
                    else
                    {
                        action = $"{method} {segments[2]}";
                    }
                }
            }

            return (action, resourceType, resourceId);
        }

        private AuditCategory DetermineCategory(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            if (path.Contains("/auth") || path.Contains("/login") || path.Contains("/logout"))
                return AuditCategory.Authentication;
            
            if (path.Contains("/users") || path.Contains("/roles") || path.Contains("/permissions"))
                return AuditCategory.Authorization;
            
            if (context.Request.Method == "GET")
                return AuditCategory.DataAccess;
            
            if (context.Request.Method == "POST" || 
                context.Request.Method == "PUT" || 
                context.Request.Method == "DELETE")
                return AuditCategory.DataModification;
            
            return AuditCategory.General;
        }

        private string BuildDetails(HttpContext context, string? requestBody, string? responseBody)
        {
            var details = new StringBuilder();
            details.AppendLine($"{context.Request.Method} {context.Request.Path}{context.Request.QueryString}");
            details.AppendLine($"Status: {context.Response.StatusCode}");

            if (!string.IsNullOrEmpty(requestBody) && _options.IncludeRequestBodyInDetails)
            {
                details.AppendLine($"Request: {TruncateBody(requestBody)}");
            }

            if (!string.IsNullOrEmpty(responseBody) && _options.IncludeResponseBodyInDetails)
            {
                details.AppendLine($"Response: {TruncateBody(responseBody)}");
            }

            return details.ToString();
        }

        private Dictionary<string, object> BuildMetadata(HttpContext context)
        {
            var metadata = new Dictionary<string, object>
            {
                ["Method"] = context.Request.Method,
                ["Path"] = context.Request.Path.Value ?? "",
                ["StatusCode"] = context.Response.StatusCode,
                ["ContentType"] = context.Request.ContentType ?? "unknown",
                ["ContentLength"] = context.Request.ContentLength ?? 0,
                ["Protocol"] = context.Request.Protocol,
                ["Scheme"] = context.Request.Scheme,
                ["Host"] = context.Request.Host.Value
            };

            // Add custom headers if configured
            foreach (var header in _options.CapturedHeaders ?? Enumerable.Empty<string>())
            {
                if (context.Request.Headers.TryGetValue(header, out var value))
                {
                    metadata[$"Header_{header}"] = value.ToString();
                }
            }

            return metadata;
        }

        private async Task<string> CaptureRequestBodyAsync(HttpRequest request)
        {
            request.EnableBuffering();
            
            using var reader = new StreamReader(
                request.Body, 
                Encoding.UTF8, 
                detectEncodingFromByteOrderMarks: false, 
                leaveOpen: true);
            
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
            
            return body;
        }

        private async Task<string> CaptureResponseBodyAsync(HttpResponse response, MemoryStream responseBodyStream)
        {
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(responseBodyStream).ReadToEndAsync();
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            return body;
        }

        private string TruncateBody(string body, int maxLength = 1000)
        {
            if (string.IsNullOrEmpty(body))
                return string.Empty;

            if (body.Length <= maxLength)
                return body;

            return body.Substring(0, maxLength) + "... (truncated)";
        }
    }

    public class AuditMiddlewareOptions
    {
        public bool CaptureRequestBody { get; set; } = true;
        public bool CaptureResponseBody { get; set; } = false;
        public bool IncludeRequestBodyInDetails { get; set; } = true;
        public bool IncludeResponseBodyInDetails { get; set; } = false;
        public string[]? ExcludedPaths { get; set; }
        public string[]? CapturedHeaders { get; set; }
        public int MaxBodyLength { get; set; } = 1000;
    }

    public static class AuditMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuditLogging(
            this IApplicationBuilder builder,
            Action<AuditMiddlewareOptions>? configureOptions = null)
        {
            var options = new AuditMiddlewareOptions();
            configureOptions?.Invoke(options);

            return builder.UseMiddleware<AuditMiddleware>(options);
        }
    }
}