using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RemoteC.Data;
using RemoteC.Data.Entities;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Services
{
    public class FileTransferService : IFileTransferService
    {
        private readonly RemoteCDbContext _context;
        private readonly ILogger<FileTransferService> _logger;
        private readonly IEncryptionService _encryptionService;
        private readonly IAuditService _auditService;
        private readonly FileTransferOptions _options;
        private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _transferLocks;
        private readonly ISessionService _sessionService;
        private readonly ConcurrentDictionary<Guid, FileTransferInfo> _activeTransfers;

        public FileTransferService(
            RemoteCDbContext context,
            ILogger<FileTransferService> logger,
            IEncryptionService encryptionService,
            IAuditService auditService,
            IOptions<FileTransferOptions> options,
            ISessionService sessionService)
        {
            _context = context;
            _logger = logger;
            _encryptionService = encryptionService;
            _auditService = auditService;
            _options = options.Value;
            _sessionService = sessionService;
            _transferLocks = new ConcurrentDictionary<Guid, SemaphoreSlim>();
            _activeTransfers = new ConcurrentDictionary<Guid, FileTransferInfo>();
        }

        public async Task<RemoteC.Shared.Models.FileTransfer> InitiateTransferAsync(FileTransferRequest request)
        {
            // Validate session
            var isValidSession = await _sessionService.ValidateSessionAsync(request.SessionId);
            if (!isValidSession)
            {
                throw new InvalidOperationException($"Invalid session: {request.SessionId}");
            }

            // Validate file size
            if (request.FileSize > _options.MaxFileSize)
            {
                throw new InvalidOperationException($"File size {request.FileSize} exceeds maximum allowed size {_options.MaxFileSize}");
            }

            // Validate file extension if configured
            if (_options.AllowedExtensions?.Length > 0)
            {
                var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
                if (!_options.AllowedExtensions.Contains(extension))
                {
                    throw new InvalidOperationException($"File extension {extension} is not allowed");
                }
            }

            // Check concurrent transfers limit
            var activeTransfers = _activeTransfers.Values.Count(t => t.Status == RemoteC.Shared.Models.TransferStatus.InProgress);
            var status = activeTransfers >= _options.MaxConcurrentTransfers ? RemoteC.Shared.Models.TransferStatus.Queued : RemoteC.Shared.Models.TransferStatus.Pending;

            // Generate encryption key if enabled
            string? encryptionKeyId = null;
            if (_options.EnableEncryption)
            {
                encryptionKeyId = await _encryptionService.GenerateKeyAsync();
            }

            // Calculate chunk information
            var totalChunks = (int)Math.Ceiling((double)request.FileSize / _options.ChunkSize);

            var transferId = Guid.NewGuid();
            var transfer = new RemoteC.Shared.Models.FileTransfer
            {
                TransferId = transferId,
                SessionId = request.SessionId,
                FileName = request.FileName,
                FileSize = request.FileSize,
                Direction = request.Direction,
                Status = status,
                TotalChunks = totalChunks,
                ReceivedChunks = 0,
                StartedAt = DateTime.UtcNow,
                Metadata = request.Metadata
            };

            // Store in memory
            var transferInfo = new FileTransferInfo
            {
                Transfer = transfer,
                UserId = request.UserId,
                EncryptionKeyId = encryptionKeyId,
                ChunkSize = _options.ChunkSize,
                Status = status,
                LastActivity = DateTime.UtcNow
            };
            _activeTransfers[transferId] = transferInfo;

            // Create database entity
            var dbTransfer = new RemoteC.Data.Entities.FileTransfer
            {
                Id = transferId,
                SessionId = request.SessionId,
                UserId = request.UserId,
                FileName = request.FileName,
                TotalSize = request.FileSize,
                ChunkSize = _options.ChunkSize,
                TotalChunks = totalChunks,
                ChunksReceived = 0,
                Direction = (RemoteC.Data.Entities.TransferDirection)request.Direction,
                Status = (RemoteC.Data.Entities.TransferStatus)status,
                EncryptionKeyId = encryptionKeyId,
                Checksum = request.Checksum,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.FileTransfers.Add(dbTransfer);
            await _context.SaveChangesAsync();

            // Create transfer directory
            var transferDir = GetTransferDirectory(transferId);
            Directory.CreateDirectory(transferDir);

            // Log transfer initiation
            await _auditService.LogAsync(new AuditLogEntry
            {
                UserId = request.UserId,
                Action = "FileTransferInitiated",
                ResourceId = transferId.ToString(),
                Details = $"FileName: {request.FileName}, FileSize: {request.FileSize}"
            });

            _logger.LogInformation("Initiated file transfer {TransferId} for file {FileName}", transferId, request.FileName);

            return transfer;
        }

        public async Task<ChunkUploadResult> UploadChunkAsync(FileChunk chunk)
        {
            if (!_activeTransfers.TryGetValue(chunk.TransferId, out var transferInfo))
            {
                return new ChunkUploadResult
                {
                    Success = false,
                    Error = "Transfer not found",
                    ErrorMessage = "Transfer not found"
                };
            }

            // Get or create lock for this transfer
            var transferLock = _transferLocks.GetOrAdd(chunk.TransferId, _ => new SemaphoreSlim(1, 1));
            
            await transferLock.WaitAsync();
            try
            {
                // Verify checksum
                if (_options.EncryptionEnabled && !string.IsNullOrEmpty(chunk.Checksum))
                {
                    var isValid = _encryptionService.VerifyChecksum(chunk.Data, chunk.Checksum);
                    if (!isValid)
                    {
                        _logger.LogWarning("Checksum verification failed for transfer {TransferId}, chunk {ChunkIndex}",
                            chunk.TransferId, chunk.ChunkIndex);
                        return new ChunkUploadResult
                        {
                            Success = false,
                            Error = "Checksum verification failed",
                            ErrorMessage = "Checksum verification failed"
                        };
                    }
                }
                // Use ChunkIndex if available, otherwise use ChunkNumber
                var chunkNumber = chunk.ChunkIndex >= 0 ? chunk.ChunkIndex : chunk.ChunkNumber;

                // Check if chunk already exists
                var chunkPath = GetChunkPath(chunk.TransferId, chunkNumber);
                if (File.Exists(chunkPath))
                {
                    // Chunk already uploaded, return success
                    return new ChunkUploadResult
                    {
                        Success = true,
                        ReceivedChunks = transferInfo.Transfer.ReceivedChunks,
                        ChunksReceived = transferInfo.Transfer.ReceivedChunks,
                        TotalChunks = transferInfo.Transfer.TotalChunks,
                        Progress = (int)((transferInfo.Transfer.ReceivedChunks * 100.0) / transferInfo.Transfer.TotalChunks),
                        IsComplete = transferInfo.Transfer.Status == RemoteC.Shared.Models.TransferStatus.Completed
                    };
                }

                // Process chunk data
                var processedData = chunk.Data;
                
                // Decompress if needed
                if (chunk.IsCompressed && _options.EnableCompression)
                {
                    processedData = await DecompressDataAsync(processedData);
                }

                // Encrypt if needed
                if (_options.EnableEncryption && !string.IsNullOrEmpty(transferInfo.EncryptionKeyId))
                {
                    processedData = await _encryptionService.EncryptAsync(processedData, transferInfo.EncryptionKeyId);
                }

                // Save chunk
                await File.WriteAllBytesAsync(chunkPath, processedData);

                // Update transfer progress
                transferInfo.Transfer.ReceivedChunks++;
                transferInfo.LastActivity = DateTime.UtcNow;
                
                if (transferInfo.Transfer.Status == RemoteC.Shared.Models.TransferStatus.Queued || 
                    transferInfo.Transfer.Status == RemoteC.Shared.Models.TransferStatus.Pending)
                {
                    transferInfo.Transfer.Status = RemoteC.Shared.Models.TransferStatus.InProgress;
                }

                // Check if all chunks received
                var isComplete = transferInfo.Transfer.ReceivedChunks == transferInfo.Transfer.TotalChunks;
                if (isComplete)
                {
                    await CompleteTransferAsync(transferInfo);
                }
                
                // Update database
                var dbTransfer = await _context.FileTransfers.FindAsync(chunk.TransferId);
                if (dbTransfer != null)
                {
                    dbTransfer.ChunksReceived = transferInfo.Transfer.ReceivedChunks;
                    dbTransfer.BytesReceived += chunk.Data.Length;
                    dbTransfer.UpdatedAt = DateTime.UtcNow;
                    dbTransfer.Status = (RemoteC.Data.Entities.TransferStatus)transferInfo.Transfer.Status;
                    if (isComplete)
                    {
                        dbTransfer.CompletedAt = DateTime.UtcNow;
                    }
                    await _context.SaveChangesAsync();
                }

                return new ChunkUploadResult
                {
                    Success = true,
                    ReceivedChunks = transferInfo.Transfer.ReceivedChunks,
                    ChunksReceived = transferInfo.Transfer.ReceivedChunks,
                    TotalChunks = transferInfo.Transfer.TotalChunks,
                    Progress = (int)((transferInfo.Transfer.ReceivedChunks * 100.0) / transferInfo.Transfer.TotalChunks),
                    IsComplete = isComplete
                };
            }
            finally
            {
                transferLock.Release();
            }
        }

        public async Task<RemoteC.Shared.Models.FileTransfer?> GetTransferStatusAsync(Guid transferId)
        {
            if (_activeTransfers.TryGetValue(transferId, out var transferInfo))
            {
                return transferInfo.Transfer;
            }
            
            // Check database
            var dbTransfer = await _context.FileTransfers.FindAsync(transferId);
            if (dbTransfer != null)
            {
                return new RemoteC.Shared.Models.FileTransfer
                {
                    TransferId = dbTransfer.Id,
                    SessionId = dbTransfer.SessionId,
                    FileName = dbTransfer.FileName,
                    FileSize = dbTransfer.TotalSize,
                    Direction = (RemoteC.Shared.Models.TransferDirection)dbTransfer.Direction,
                    Status = (RemoteC.Shared.Models.TransferStatus)dbTransfer.Status,
                    TotalChunks = dbTransfer.TotalChunks,
                    ReceivedChunks = dbTransfer.ChunksReceived,
                    StartedAt = dbTransfer.CreatedAt,
                    CompletedAt = dbTransfer.CompletedAt
                };
            }
            
            return null;
        }

        public async Task<FileChunk?> DownloadChunkAsync(Guid transferId, int chunkIndex)
        {
            if (!_activeTransfers.TryGetValue(transferId, out var transferInfo))
            {
                return null;
            }

            // For download operations, we would read from the stored file
            // This is a placeholder implementation
            var chunkData = new byte[Math.Min(_options.ChunkSize, (int)(transferInfo.Transfer.FileSize - (chunkIndex * _options.ChunkSize)))];
            
            return new FileChunk
            {
                TransferId = transferId,
                ChunkIndex = chunkIndex,
                ChunkNumber = chunkIndex,
                Data = chunkData,
                Checksum = _encryptionService.ComputeChecksum(chunkData)
            };
        }

        public async Task<bool> CancelTransferAsync(Guid transferId)
        {
            if (!_activeTransfers.TryGetValue(transferId, out var transferInfo))
            {
                return false;
            }

            transferInfo.Transfer.Status = RemoteC.Shared.Models.TransferStatus.Cancelled;
            
            // Update database
            var dbTransfer = await _context.FileTransfers.FindAsync(transferId);
            if (dbTransfer != null)
            {
                dbTransfer.Status = RemoteC.Data.Entities.TransferStatus.Cancelled;
                dbTransfer.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // Clean up transfer directory
            var transferDir = GetTransferDirectory(transferId);
            if (Directory.Exists(transferDir))
            {
                Directory.Delete(transferDir, recursive: true);
            }

            // Remove from active transfers
            _activeTransfers.TryRemove(transferId, out _);
            _transferLocks.TryRemove(transferId, out _);

            _logger.LogInformation("Cancelled transfer {TransferId}", transferId);
            return true;
        }

        public async Task<int> CleanupStalledTransfersAsync(TimeSpan stalledThreshold)
        {
            var cutoffTime = DateTime.UtcNow - stalledThreshold;
            
            var stalledTransfers = await _context.FileTransfers
                .Where(t => t.Status == RemoteC.Data.Entities.TransferStatus.InProgress && t.UpdatedAt < cutoffTime)
                .ToListAsync();

            foreach (var transfer in stalledTransfers)
            {
                transfer.Status = RemoteC.Data.Entities.TransferStatus.Failed;
                transfer.UpdatedAt = DateTime.UtcNow;
                
                _logger.LogWarning("Marking transfer {TransferId} as failed due to inactivity", transfer.Id);
            }

            await _context.SaveChangesAsync();

            return stalledTransfers.Count;
        }

        private bool ValidateChunk(FileChunk chunk)
        {
            var computedChecksum = ComputeChecksum(chunk.Data);
            return computedChecksum == chunk.Checksum;
        }

        private string ComputeChecksum(byte[] data)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(data);
            return Convert.ToBase64String(hash);
        }

        private int CalculateProgress(RemoteC.Data.Entities.FileTransfer transfer)
        {
            if (transfer.TotalChunks == 0) return 0;
            return (int)((transfer.ChunksReceived * 100) / transfer.TotalChunks);
        }

        private string GetTransferDirectory(Guid transferId)
        {
            return Path.Combine(_options.StoragePath, transferId.ToString());
        }

        private string GetChunkPath(Guid transferId, int chunkNumber)
        {
            return Path.Combine(GetTransferDirectory(transferId), $"chunk_{chunkNumber}");
        }

        public async Task<IEnumerable<int>> GetMissingChunksAsync(Guid transferId)
        {
            if (!_activeTransfers.TryGetValue(transferId, out var transferInfo))
            {
                return Enumerable.Empty<int>();
            }

            var receivedChunks = new HashSet<int>();
            var transferDir = GetTransferDirectory(transferId);
            
            for (int i = 0; i < transferInfo.Transfer.TotalChunks; i++)
            {
                var chunkPath = GetChunkPath(transferId, i);
                if (File.Exists(chunkPath))
                {
                    receivedChunks.Add(i);
                }
            }
            
            var missingChunks = new List<int>();
            for (int i = 0; i < transferInfo.Transfer.TotalChunks; i++)
            {
                if (!receivedChunks.Contains(i))
                {
                    missingChunks.Add(i);
                }
            }
            
            return missingChunks;
        }

        private async Task<List<int>> GetMissingChunksInternalAsync(RemoteC.Data.Entities.FileTransfer transfer)
        {
            var missingChunks = new List<int>();
            var transferDir = GetTransferDirectory(transfer.Id);

            for (int i = 0; i < transfer.TotalChunks; i++)
            {
                var chunkPath = GetChunkPath(transfer.Id, i);
                if (!File.Exists(chunkPath))
                {
                    missingChunks.Add(i);
                }
            }

            return missingChunks;
        }

        private async Task AssembleFileAsync(RemoteC.Data.Entities.FileTransfer transfer)
        {
            var completedDir = Path.Combine(_options.StoragePath, "completed");
            Directory.CreateDirectory(completedDir);

            var finalPath = Path.Combine(completedDir, $"{transfer.Id}_{transfer.FileName}");
            
            using (var outputStream = new FileStream(finalPath, FileMode.Create, FileAccess.Write))
            {
                for (int i = 0; i < transfer.TotalChunks; i++)
                {
                    var chunkPath = GetChunkPath(transfer.Id, i);
                    var chunkData = await File.ReadAllBytesAsync(chunkPath);

                    // Decrypt if needed
                    if (_options.EnableEncryption && !string.IsNullOrEmpty(transfer.EncryptionKeyId))
                    {
                        chunkData = await _encryptionService.DecryptAsync(chunkData, transfer.EncryptionKeyId);
                    }

                    await outputStream.WriteAsync(chunkData, 0, chunkData.Length);
                }
            }

            // Verify final file checksum if provided
            if (!string.IsNullOrEmpty(transfer.Checksum))
            {
                var fileData = await File.ReadAllBytesAsync(finalPath);
                var computedChecksum = ComputeChecksum(fileData);
                
                if (computedChecksum != transfer.Checksum)
                {
                    File.Delete(finalPath);
                    throw new InvalidOperationException("Final file checksum mismatch");
                }
            }

            // Clean up chunk files
            var transferDir = GetTransferDirectory(transfer.Id);
            Directory.Delete(transferDir, recursive: true);

            _logger.LogInformation("Assembled file {FileName} for transfer {TransferId}", transfer.FileName, transfer.Id);
        }

        private async Task<byte[]> CompressDataAsync(byte[] data)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
            {
                await gzip.WriteAsync(data, 0, data.Length);
            }
            return output.ToArray();
        }

        private async Task<byte[]> DecompressDataAsync(byte[] data)
        {
            using var input = new MemoryStream(data);
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            {
                await gzip.CopyToAsync(output);
            }
            return output.ToArray();
        }

        public async Task<FileTransferMetrics> GetTransferMetricsAsync(Guid transferId)
        {
            if (!_activeTransfers.TryGetValue(transferId, out var transferInfo))
            {
                throw new InvalidOperationException($"Transfer {transferId} not found");
            }

            var transfer = transferInfo.Transfer;
            var elapsed = DateTime.UtcNow - transfer.StartedAt;
            var bytesTransferred = (long)transfer.ReceivedChunks * _options.ChunkSize;
            bytesTransferred = Math.Min(bytesTransferred, transfer.FileSize);

            var transferRate = elapsed.TotalSeconds > 0 ? bytesTransferred / elapsed.TotalSeconds : 0;
            var remainingBytes = transfer.FileSize - bytesTransferred;
            var estimatedTimeRemaining = transferRate > 0 
                ? TimeSpan.FromSeconds(remainingBytes / transferRate) 
                : TimeSpan.MaxValue;

            return new FileTransferMetrics
            {
                TransferId = transferId,
                BytesTransferred = bytesTransferred,
                TotalBytes = transfer.FileSize,
                ChunksTransferred = transfer.ReceivedChunks,
                TotalChunks = transfer.TotalChunks,
                TransferRate = transferRate,
                ElapsedTime = elapsed,
                EstimatedTimeRemaining = estimatedTimeRemaining,
                Status = transfer.Status,
                ProgressPercentage = (double)bytesTransferred / transfer.FileSize * 100
            };
        }


        public async Task<int> CleanupStalledTransfersAsync()
        {
            // Default to 60 minutes for stalled threshold
            return await CleanupStalledTransfersAsync(TimeSpan.FromMinutes(60));
        }

        public async Task CleanupExpiredTransfersAsync()
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-24);
            var expiredTransfers = await _context.FileTransfers
                .Where(t => t.CreatedAt < cutoffTime && 
                           (t.Status == RemoteC.Data.Entities.TransferStatus.Pending || t.Status == RemoteC.Data.Entities.TransferStatus.InProgress))
                .ToListAsync();

            foreach (var transfer in expiredTransfers)
            {
                transfer.Status = RemoteC.Data.Entities.TransferStatus.Failed;
                transfer.UpdatedAt = DateTime.UtcNow;
                
                await _auditService.LogActionAsync(
                    "file_transfer.expired",
                    "FileTransfer",
                    transfer.Id.ToString(),
                    null,
                    null,
                    new { reason = "Transfer expired after 24 hours" }
                );
            }

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Cleaned up {Count} expired transfers", expiredTransfers.Count);
        }

        private async Task CompleteTransferAsync(FileTransferInfo transferInfo)
        {
            try
            {
                // Assemble chunks into final file
                var finalPath = Path.Combine(_options.StoragePath, $"{transferInfo.Transfer.TransferId}_{transferInfo.Transfer.FileName}");

                if (!Directory.Exists(_options.StoragePath))
                {
                    Directory.CreateDirectory(_options.StoragePath);
                }

                using (var fileStream = new FileStream(finalPath, FileMode.Create))
                {
                    for (int i = 0; i < transferInfo.Transfer.TotalChunks; i++)
                    {
                        var chunkPath = GetChunkPath(transferInfo.Transfer.TransferId, i);
                        if (File.Exists(chunkPath))
                        {
                            var chunkData = await File.ReadAllBytesAsync(chunkPath);
                            
                            // Decrypt if needed
                            if (_options.EnableEncryption && !string.IsNullOrEmpty(transferInfo.EncryptionKeyId))
                            {
                                chunkData = await _encryptionService.DecryptAsync(chunkData, transferInfo.EncryptionKeyId);
                            }
                            
                            await fileStream.WriteAsync(chunkData, 0, chunkData.Length);
                        }
                    }
                }

                // Update transfer status
                transferInfo.Transfer.Status = RemoteC.Shared.Models.TransferStatus.Completed;
                transferInfo.Transfer.CompletedAt = DateTime.UtcNow;

                // Clean up chunks
                var transferDir = GetTransferDirectory(transferInfo.Transfer.TransferId);
                if (Directory.Exists(transferDir))
                {
                    Directory.Delete(transferDir, recursive: true);
                }

                _logger.LogInformation("File transfer completed: {TransferId}, file saved to: {FinalPath}",
                    transferInfo.Transfer.TransferId, finalPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete file transfer {TransferId}", transferInfo.Transfer.TransferId);
                transferInfo.Transfer.Status = RemoteC.Shared.Models.TransferStatus.Failed;
                transferInfo.Transfer.Error = ex.Message;
            }
        }

        private class FileTransferInfo
        {
            public RemoteC.Shared.Models.FileTransfer Transfer { get; set; } = null!;
            public Guid UserId { get; set; }
            public string? EncryptionKeyId { get; set; }
            public int ChunkSize { get; set; }
            public RemoteC.Shared.Models.TransferStatus Status { get; set; }
            public DateTime LastActivity { get; set; }
        }
    }
}