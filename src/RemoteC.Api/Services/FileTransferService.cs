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

        public FileTransferService(
            RemoteCDbContext context,
            ILogger<FileTransferService> logger,
            IEncryptionService encryptionService,
            IAuditService auditService,
            IOptions<FileTransferOptions> options)
        {
            _context = context;
            _logger = logger;
            _encryptionService = encryptionService;
            _auditService = auditService;
            _options = options.Value;
            _transferLocks = new ConcurrentDictionary<Guid, SemaphoreSlim>();
        }

        public async Task<FileTransfer> InitiateTransferAsync(FileTransferRequest request)
        {
            // Validate file size
            if (request.FileSize > _options.MaxFileSize)
            {
                throw new InvalidOperationException($"File size {request.FileSize} exceeds maximum allowed size {_options.MaxFileSize}");
            }

            // Generate encryption key if enabled
            string? encryptionKeyId = null;
            if (_options.EnableEncryption)
            {
                encryptionKeyId = await _encryptionService.GenerateKeyAsync();
            }

            // Calculate chunk information
            var totalChunks = (int)Math.Ceiling((double)request.FileSize / _options.ChunkSize);

            var transfer = new FileTransfer
            {
                Id = Guid.NewGuid(),
                SessionId = request.SessionId,
                UserId = request.UserId,
                FileName = request.FileName,
                TotalSize = request.FileSize,
                ChunkSize = _options.ChunkSize,
                TotalChunks = totalChunks,
                ChunksReceived = 0,
                Direction = (RemoteC.Data.Entities.TransferDirection)request.Direction,
                Status = RemoteC.Data.Entities.TransferStatus.Pending,
                EncryptionKeyId = encryptionKeyId,
                Checksum = request.Checksum,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.FileTransfers.Add(transfer);
            await _context.SaveChangesAsync();

            // Create transfer directory
            var transferDir = GetTransferDirectory(transfer.Id);
            Directory.CreateDirectory(transferDir);

            // Log transfer initiation
            await _auditService.LogAsync(new AuditLogEntry
            {
                UserId = request.UserId,
                Action = "FileTransferInitiated",
                ResourceId = transfer.Id.ToString(),
                Details = $"FileName: {request.FileName}, FileSize: {request.FileSize}"
            });

            _logger.LogInformation("Initiated file transfer {TransferId} for file {FileName}", transfer.Id, request.FileName);

            return transfer;
        }

        public async Task<ChunkUploadResult> UploadChunkAsync(FileChunk chunk)
        {
            var transfer = await _context.FileTransfers.FindAsync(chunk.TransferId);
            if (transfer == null)
            {
                throw new InvalidOperationException($"Transfer {chunk.TransferId} not found");
            }

            // Get or create lock for this transfer
            var transferLock = _transferLocks.GetOrAdd(transfer.Id, _ => new SemaphoreSlim(1, 1));
            
            await transferLock.WaitAsync();
            try
            {
                // Validate chunk
                if (!ValidateChunk(chunk))
                {
                    return new ChunkUploadResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid chunk checksum"
                    };
                }

                // Check if chunk already exists
                var chunkPath = GetChunkPath(transfer.Id, chunk.ChunkNumber);
                if (File.Exists(chunkPath))
                {
                    // Chunk already uploaded, return success
                    return new ChunkUploadResult
                    {
                        Success = true,
                        ChunksReceived = transfer.ChunksReceived,
                        BytesReceived = transfer.BytesReceived,
                        Progress = CalculateProgress(transfer),
                        IsComplete = transfer.Status == RemoteC.Data.Entities.TransferStatus.Completed
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
                if (_options.EnableEncryption && !string.IsNullOrEmpty(transfer.EncryptionKeyId))
                {
                    processedData = await _encryptionService.EncryptAsync(processedData, transfer.EncryptionKeyId);
                }

                // Save chunk
                await File.WriteAllBytesAsync(chunkPath, processedData);

                // Update transfer progress
                transfer.ChunksReceived++;
                transfer.BytesReceived += chunk.Data.Length;
                transfer.UpdatedAt = DateTime.UtcNow;

                if (transfer.ChunksReceived == transfer.TotalChunks)
                {
                    // All chunks received, assemble file
                    await AssembleFileAsync(transfer);
                    transfer.Status = RemoteC.Data.Entities.TransferStatus.Completed;
                    transfer.CompletedAt = DateTime.UtcNow;
                }
                else
                {
                    transfer.Status = RemoteC.Data.Entities.TransferStatus.InProgress;
                }

                await _context.SaveChangesAsync();

                return new ChunkUploadResult
                {
                    Success = true,
                    ChunksReceived = transfer.ChunksReceived,
                    BytesReceived = transfer.BytesReceived,
                    Progress = CalculateProgress(transfer),
                    IsComplete = transfer.Status == RemoteC.Data.Entities.TransferStatus.Completed
                };
            }
            finally
            {
                transferLock.Release();
            }
        }

        public async Task<FileTransfer?> GetTransferStatusAsync(Guid transferId)
        {
            return await _context.FileTransfers.FindAsync(transferId);
        }

        public async Task<FileChunk?> DownloadChunkAsync(Guid transferId, int chunkIndex)
        {
            var transfer = await _context.FileTransfers.FindAsync(transferId);
            if (transfer == null || transfer.Direction != RemoteC.Data.Entities.TransferDirection.Download)
            {
                return null;
            }

            if (chunkIndex < 0 || chunkIndex >= transfer.TotalChunks)
            {
                return null;
            }

            if (string.IsNullOrEmpty(transfer.SourcePath) || !File.Exists(transfer.SourcePath))
            {
                throw new InvalidOperationException("Source file not found");
            }

            // Calculate chunk boundaries
            var startOffset = (long)chunkIndex * transfer.ChunkSize;
            var chunkSize = (int)Math.Min(transfer.ChunkSize, transfer.TotalSize - startOffset);

            // Read chunk from file
            var chunkData = new byte[chunkSize];
            using (var fileStream = new FileStream(transfer.SourcePath, FileMode.Open, FileAccess.Read))
            {
                fileStream.Seek(startOffset, SeekOrigin.Begin);
                await fileStream.ReadAsync(chunkData, 0, chunkSize);
            }

            // Encrypt if needed
            if (_options.EnableEncryption && !string.IsNullOrEmpty(transfer.EncryptionKeyId))
            {
                chunkData = await _encryptionService.EncryptAsync(chunkData, transfer.EncryptionKeyId);
            }

            // Compress if needed
            if (_options.EnableCompression)
            {
                chunkData = await CompressDataAsync(chunkData);
            }

            return new FileChunk
            {
                TransferId = transferId,
                ChunkNumber = chunkIndex,
                Data = chunkData,
                Checksum = ComputeChecksum(chunkData),
                IsCompressed = _options.EnableCompression
            };
        }

        public async Task<bool> CancelTransferAsync(Guid transferId)
        {
            var transfer = await _context.FileTransfers.FindAsync(transferId);
            if (transfer == null)
            {
                return false;
            }

            transfer.Status = RemoteC.Data.Entities.TransferStatus.Cancelled;
            transfer.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Clean up transfer directory
            var transferDir = GetTransferDirectory(transferId);
            if (Directory.Exists(transferDir))
            {
                Directory.Delete(transferDir, recursive: true);
            }

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

        private int CalculateProgress(FileTransfer transfer)
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
            var transfer = await _context.FileTransfers.FindAsync(transferId);
            if (transfer == null)
            {
                throw new InvalidOperationException($"Transfer {transferId} not found");
            }
            
            return await GetMissingChunksInternalAsync(transfer);
        }

        private async Task<List<int>> GetMissingChunksInternalAsync(FileTransfer transfer)
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

        private async Task AssembleFileAsync(FileTransfer transfer)
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
            var transfer = await _context.FileTransfers.FindAsync(transferId);
            if (transfer == null)
            {
                throw new InvalidOperationException($"Transfer {transferId} not found");
            }

            var elapsedTime = transfer.UpdatedAt - transfer.CreatedAt;
            var bytesTransferred = (long)(transfer.ChunksReceived * transfer.ChunkSize);
            var transferRate = elapsedTime.TotalSeconds > 0 ? bytesTransferred / elapsedTime.TotalSeconds : 0;
            var remainingBytes = transfer.TotalSize - bytesTransferred;
            var estimatedTimeRemaining = transferRate > 0 
                ? TimeSpan.FromSeconds(remainingBytes / transferRate) 
                : TimeSpan.Zero;

            return new FileTransferMetrics
            {
                TransferId = transfer.Id,
                BytesTransferred = bytesTransferred,
                TotalBytes = transfer.TotalSize,
                ChunksTransferred = transfer.ChunksReceived,
                TotalChunks = transfer.TotalChunks,
                TransferRate = transferRate,
                ElapsedTime = elapsedTime,
                EstimatedTimeRemaining = estimatedTimeRemaining,
                RetryCount = 0, // TODO: Track retry count
                Status = (RemoteC.Shared.Models.TransferStatus)transfer.Status
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

    }
}