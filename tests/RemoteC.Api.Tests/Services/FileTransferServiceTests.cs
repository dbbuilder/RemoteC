using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RemoteC.Api.Services;
using RemoteC.Data;
using RemoteC.Data.Entities;
using RemoteC.Shared.Models;
using Xunit;

namespace RemoteC.Api.Tests.Services
{
    public class FileTransferServiceTests : IDisposable
    {
        private readonly RemoteCDbContext _context;
        private readonly Mock<ILogger<FileTransferService>> _loggerMock;
        private readonly Mock<IEncryptionService> _encryptionMock;
        private readonly Mock<IAuditService> _auditMock;
        private readonly FileTransferService _service;
        private readonly string _testDirectory;
        private readonly FileTransferOptions _options;

        public FileTransferServiceTests()
        {
            // Setup in-memory database
            var dbOptions = new DbContextOptionsBuilder<RemoteCDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new RemoteCDbContext(dbOptions);

            // Setup test directory
            _testDirectory = Path.Combine(Path.GetTempPath(), $"FileTransferTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);

            // Setup mocks
            _loggerMock = new Mock<ILogger<FileTransferService>>();
            _encryptionMock = new Mock<IEncryptionService>();
            _auditMock = new Mock<IAuditService>();

            // Setup encryption mock to return data as-is for testing
            _encryptionMock.Setup(e => e.EncryptAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync((byte[] data, string keyId) => data);
            _encryptionMock.Setup(e => e.DecryptAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync((byte[] data, string keyId) => data);
            _encryptionMock.Setup(e => e.GenerateKeyAsync())
                .ReturnsAsync("test-key-id");

            // Setup options
            _options = new FileTransferOptions
            {
                ChunkSize = 1024 * 1024, // 1MB chunks
                MaxFileSize = 100 * 1024 * 1024, // 100MB
                StoragePath = _testDirectory,
                EnableEncryption = true,
                EnableCompression = true,
                MaxConcurrentTransfers = 5,
                ChunkRetryCount = 3,
                ChunkRetryDelayMs = 100
            };

            // Create service
            _service = new FileTransferService(
                _context,
                _loggerMock.Object,
                _encryptionMock.Object,
                _auditMock.Object,
                Options.Create(_options));
        }

        #region Transfer Initialization Tests

        [Fact]
        public async Task InitiateTransferAsync_ValidFile_CreatesTransferRecord()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var filePath = CreateTestFile("test.txt", "Hello, World!");
            
            var request = new FileTransferRequest
            {
                SessionId = sessionId,
                UserId = userId,
                FileName = "test.txt",
                FileSize = new FileInfo(filePath).Length,
                Direction = TransferDirection.Upload
            };

            // Act
            var result = await _service.InitiateTransferAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(RemoteC.Shared.Models.TransferStatus.Pending, result.Status);
            Assert.Equal(request.FileName, result.FileName);
            Assert.Equal(request.FileSize, result.TotalSize);
            Assert.True(result.TotalChunks > 0);
            
            // Verify database record
            var dbTransfer = await _context.FileTransfers.FirstAsync();
            Assert.Equal(result.Id, dbTransfer.Id);
        }

        [Fact]
        public async Task InitiateTransferAsync_FileTooLarge_ThrowsException()
        {
            // Arrange
            var request = new FileTransferRequest
            {
                SessionId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                FileName = "huge.bin",
                FileSize = _options.MaxFileSize + 1,
                Direction = TransferDirection.Upload
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.InitiateTransferAsync(request));
        }

        [Fact]
        public async Task InitiateTransferAsync_CalculatesCorrectChunkCount()
        {
            // Arrange
            var fileSize = (long)(_options.ChunkSize * 2.5); // 2.5 chunks
            var request = new FileTransferRequest
            {
                SessionId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                FileName = "test.bin",
                FileSize = fileSize,
                Direction = TransferDirection.Upload
            };

            // Act
            var result = await _service.InitiateTransferAsync(request);

            // Assert
            Assert.Equal(3, result.TotalChunks); // Should round up to 3 chunks
        }

        #endregion

        #region Chunk Upload Tests

        [Fact]
        public async Task UploadChunkAsync_ValidChunk_SavesAndUpdatesProgress()
        {
            // Arrange
            var transfer = await CreateTestTransfer();
            var chunkData = Encoding.UTF8.GetBytes("Chunk data");
            
            var chunk = new FileChunk
            {
                TransferId = transfer.Id,
                ChunkNumber = 0,
                Data = chunkData,
                Checksum = ComputeChecksum(chunkData)
            };

            // Act
            var result = await _service.UploadChunkAsync(chunk);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, result.ChunksReceived);
            Assert.Equal(chunkData.Length, result.BytesReceived);
            
            // Verify file was written
            var chunkPath = Path.Combine(_testDirectory, transfer.Id.ToString(), "chunk_0");
            Assert.True(File.Exists(chunkPath));
        }

        [Fact]
        public async Task UploadChunkAsync_InvalidChecksum_RejectsChunk()
        {
            // Arrange
            var transfer = await CreateTestTransfer();
            var chunk = new FileChunk
            {
                TransferId = transfer.Id,
                ChunkNumber = 0,
                Data = Encoding.UTF8.GetBytes("Chunk data"),
                Checksum = "invalid-checksum"
            };

            // Act
            var result = await _service.UploadChunkAsync(chunk);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("checksum", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UploadChunkAsync_DuplicateChunk_IgnoresButReturnsSuccess()
        {
            // Arrange
            var transfer = await CreateTestTransfer();
            var chunkData = Encoding.UTF8.GetBytes("Chunk data");
            var chunk = new FileChunk
            {
                TransferId = transfer.Id,
                ChunkNumber = 0,
                Data = chunkData,
                Checksum = ComputeChecksum(chunkData)
            };

            // Upload first time
            await _service.UploadChunkAsync(chunk);

            // Act - Upload same chunk again
            var result = await _service.UploadChunkAsync(chunk);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, result.ChunksReceived); // Should still be 1, not 2
        }

        [Fact]
        public async Task UploadChunkAsync_AllChunksReceived_CompletesTransfer()
        {
            // Arrange
            var fileContent = "Complete file content";
            var transfer = await CreateTestTransfer(fileContent.Length, chunkSize: 10);
            var chunks = SplitIntoChunks(fileContent, 10);

            // Act - Upload all chunks
            foreach (var (chunkData, index) in chunks.Select((c, i) => (c, i)))
            {
                var chunk = new FileChunk
                {
                    TransferId = transfer.Id,
                    ChunkNumber = index,
                    Data = Encoding.UTF8.GetBytes(chunkData),
                    Checksum = ComputeChecksum(Encoding.UTF8.GetBytes(chunkData))
                };
                await _service.UploadChunkAsync(chunk);
            }

            // Assert
            var updatedTransfer = await _context.FileTransfers.FindAsync(transfer.Id);
            Assert.Equal(RemoteC.Shared.Models.TransferStatus.Completed, updatedTransfer!.Status);
            Assert.Equal(100, updatedTransfer.Progress);
            
            // Verify final file exists
            var finalPath = Path.Combine(_testDirectory, "completed", $"{transfer.Id}_{transfer.FileName}");
            Assert.True(File.Exists(finalPath));
            Assert.Equal(fileContent, File.ReadAllText(finalPath));
        }

        #endregion

        #region Resume Tests

        [Fact]
        public async Task GetTransferStatusAsync_ReturnsCorrectMissingChunks()
        {
            // Arrange
            var transfer = await CreateTestTransfer(totalChunks: 5);
            
            // Upload chunks 0, 2, and 4 (missing 1 and 3)
            foreach (var chunkNum in new[] { 0, 2, 4 })
            {
                await UploadTestChunk(transfer.Id, chunkNum);
            }

            // Act
            var status = await _service.GetTransferStatusAsync(transfer.Id);

            // Assert
            Assert.NotNull(status);
            Assert.Equal(3, status.ChunksReceived);
            Assert.Equal(new[] { 1, 3 }, status.MissingChunks);
            Assert.Equal(60, status.Progress); // 3 out of 5 chunks
        }

        [Fact]
        public async Task ResumeTransferAsync_UploadsOnlyMissingChunks()
        {
            // Arrange
            var fileContent = "This is a test file for resume functionality";
            var transfer = await CreateTestTransfer(fileContent.Length, chunkSize: 10);
            var chunks = SplitIntoChunks(fileContent, 10);
            
            // Upload only even-numbered chunks
            for (int i = 0; i < chunks.Count; i += 2)
            {
                await UploadTestChunk(transfer.Id, i, chunks[i]);
            }

            // Act - Resume with only missing chunks
            var missingChunks = new List<FileChunk>();
            for (int i = 1; i < chunks.Count; i += 2)
            {
                missingChunks.Add(new FileChunk
                {
                    TransferId = transfer.Id,
                    ChunkNumber = i,
                    Data = Encoding.UTF8.GetBytes(chunks[i]),
                    Checksum = ComputeChecksum(Encoding.UTF8.GetBytes(chunks[i]))
                });
            }

            foreach (var chunk in missingChunks)
            {
                await _service.UploadChunkAsync(chunk);
            }

            // Assert
            var updatedTransfer = await _context.FileTransfers.FindAsync(transfer.Id);
            Assert.Equal(RemoteC.Shared.Models.TransferStatus.Completed, updatedTransfer!.Status);
            
            var finalPath = Path.Combine(_testDirectory, "completed", $"{transfer.Id}_{transfer.FileName}");
            Assert.Equal(fileContent, File.ReadAllText(finalPath));
        }

        #endregion

        #region Download Tests

        [Fact]
        public async Task DownloadChunkAsync_ValidRequest_ReturnsChunkData()
        {
            // Arrange
            var fileContent = "Download test content";
            var filePath = CreateTestFile("download.txt", fileContent);
            var transfer = await CreateTestTransfer(
                fileContent.Length, 
                direction: RemoteC.Shared.Models.TransferDirection.Download,
                sourcePath: filePath);

            // Act
            var chunk = await _service.DownloadChunkAsync(transfer.Id, 0);

            // Assert
            Assert.NotNull(chunk);
            Assert.Equal(0, chunk.ChunkNumber);
            Assert.Equal(fileContent, Encoding.UTF8.GetString(chunk.Data));
            Assert.Equal(ComputeChecksum(chunk.Data), chunk.Checksum);
        }

        [Fact]
        public async Task DownloadChunkAsync_ChunkOutOfRange_ReturnsNull()
        {
            // Arrange
            var transfer = await CreateTestTransfer(totalChunks: 3, direction: TransferDirection.Download);

            // Act
            var chunk = await _service.DownloadChunkAsync(transfer.Id, 5);

            // Assert
            Assert.Null(chunk);
        }

        [Fact]
        public async Task DownloadChunkAsync_SupportsPartialDownload()
        {
            // Arrange
            var fileContent = string.Concat(Enumerable.Repeat("1234567890", 10)); // 100 chars
            var filePath = CreateTestFile("partial.txt", fileContent);
            var transfer = await CreateTestTransfer(
                fileContent.Length,
                chunkSize: 25,
                direction: RemoteC.Shared.Models.TransferDirection.Download,
                sourcePath: filePath);

            // Act - Download chunks 1 and 3 (skip 0 and 2)
            var chunk1 = await _service.DownloadChunkAsync(transfer.Id, 1);
            var chunk3 = await _service.DownloadChunkAsync(transfer.Id, 3);

            // Assert
            Assert.Equal(fileContent.Substring(25, 25), Encoding.UTF8.GetString(chunk1!.Data));
            Assert.Equal(fileContent.Substring(75, 25), Encoding.UTF8.GetString(chunk3!.Data));
        }

        #endregion

        #region Compression Tests

        [Fact]
        public async Task UploadChunkAsync_WithCompression_CompressesData()
        {
            // Arrange
            var transfer = await CreateTestTransfer();
            var uncompressedData = string.Concat(Enumerable.Repeat("AAAA", 1000)); // Highly compressible
            var chunk = new FileChunk
            {
                TransferId = transfer.Id,
                ChunkNumber = 0,
                Data = Encoding.UTF8.GetBytes(uncompressedData),
                Checksum = ComputeChecksum(Encoding.UTF8.GetBytes(uncompressedData)),
                IsCompressed = true
            };

            // Act
            var result = await _service.UploadChunkAsync(chunk);

            // Assert
            Assert.True(result.Success);
            
            // Verify compressed size is smaller
            var chunkPath = Path.Combine(_testDirectory, transfer.Id.ToString(), "chunk_0");
            var savedSize = new FileInfo(chunkPath).Length;
            Assert.True(savedSize < uncompressedData.Length);
        }

        #endregion

        #region Concurrency Tests

        [Fact]
        public async Task UploadChunkAsync_ConcurrentUploads_HandlesCorrectly()
        {
            // Arrange
            var transfer = await CreateTestTransfer(totalChunks: 20);
            var tasks = new List<Task<ChunkUploadResult>>();

            // Act - Upload all chunks concurrently
            for (int i = 0; i < 20; i++)
            {
                var chunkNum = i; // Capture loop variable
                var task = Task.Run(async () =>
                {
                    var data = Encoding.UTF8.GetBytes($"Chunk {chunkNum}");
                    var chunk = new FileChunk
                    {
                        TransferId = transfer.Id,
                        ChunkNumber = chunkNum,
                        Data = data,
                        Checksum = ComputeChecksum(data)
                    };
                    return await _service.UploadChunkAsync(chunk);
                });
                tasks.Add(task);
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, r => Assert.True(r.Success));
            
            var updatedTransfer = await _context.FileTransfers.FindAsync(transfer.Id);
            Assert.Equal(RemoteC.Shared.Models.TransferStatus.Completed, updatedTransfer!.Status);
            Assert.Equal(20, updatedTransfer.ChunksReceived);
        }

        #endregion

        #region Cleanup Tests

        [Fact]
        public async Task CancelTransferAsync_DeletesPartialData()
        {
            // Arrange
            var transfer = await CreateTestTransfer();
            await UploadTestChunk(transfer.Id, 0);
            
            var transferDir = Path.Combine(_testDirectory, transfer.Id.ToString());
            Assert.True(Directory.Exists(transferDir));

            // Act
            await _service.CancelTransferAsync(transfer.Id);

            // Assert
            Assert.False(Directory.Exists(transferDir));
            
            var dbTransfer = await _context.FileTransfers.FindAsync(transfer.Id);
            Assert.Equal(RemoteC.Data.Entities.TransferStatus.Cancelled, dbTransfer!.Status);
        }

        [Fact]
        public async Task CleanupStalledTransfersAsync_RemovesOldTransfers()
        {
            // Arrange
            var oldTransfer = await CreateTestTransfer();
            var recentTransfer = await CreateTestTransfer();
            
            // Make old transfer stalled
            oldTransfer.UpdatedAt = DateTime.UtcNow.AddHours(-2);
            await _context.SaveChangesAsync();

            // Act
            var cleaned = await _service.CleanupStalledTransfersAsync(TimeSpan.FromHours(1));

            // Assert
            Assert.Equal(1, cleaned);
            
            var oldDbTransfer = await _context.FileTransfers.FindAsync(oldTransfer.Id);
            Assert.Equal(RemoteC.Data.Entities.TransferStatus.Failed, oldDbTransfer!.Status);
            
            var recentDbTransfer = await _context.FileTransfers.FindAsync(recentTransfer.Id);
            Assert.Equal(RemoteC.Data.Entities.TransferStatus.InProgress, recentDbTransfer!.Status);
        }

        #endregion

        #region Helper Methods

        private async Task<FileTransfer> CreateTestTransfer(
            long fileSize = 1000,
            int chunkSize = 100,
            int? totalChunks = null,
            RemoteC.Shared.Models.TransferDirection direction = RemoteC.Shared.Models.TransferDirection.Upload,
            string? sourcePath = null)
        {
            var transfer = new FileTransfer
            {
                Id = Guid.NewGuid(),
                SessionId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                FileName = "test.dat",
                TotalSize = fileSize,
                ChunkSize = chunkSize,
                TotalChunks = totalChunks ?? (int)Math.Ceiling((double)fileSize / chunkSize),
                Direction = (RemoteC.Data.Entities.TransferDirection)direction,
                Status = RemoteC.Data.Entities.TransferStatus.InProgress,
                EncryptionKeyId = "test-key",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SourcePath = sourcePath
            };

            _context.FileTransfers.Add(transfer);
            await _context.SaveChangesAsync();
            
            // Create transfer directory
            var transferDir = Path.Combine(_testDirectory, transfer.Id.ToString());
            Directory.CreateDirectory(transferDir);
            
            return transfer;
        }

        private string CreateTestFile(string fileName, string content)
        {
            var filePath = Path.Combine(_testDirectory, fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }

        private async Task UploadTestChunk(Guid transferId, int chunkNumber, string? content = null)
        {
            var data = Encoding.UTF8.GetBytes(content ?? $"Chunk {chunkNumber}");
            var chunk = new FileChunk
            {
                TransferId = transferId,
                ChunkNumber = chunkNumber,
                Data = data,
                Checksum = ComputeChecksum(data)
            };
            await _service.UploadChunkAsync(chunk);
        }

        private List<string> SplitIntoChunks(string content, int chunkSize)
        {
            var chunks = new List<string>();
            for (int i = 0; i < content.Length; i += chunkSize)
            {
                chunks.Add(content.Substring(i, Math.Min(chunkSize, content.Length - i)));
            }
            return chunks;
        }

        private string ComputeChecksum(byte[] data)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(data);
            return Convert.ToBase64String(hash);
        }

        public void Dispose()
        {
            _context?.Dispose();
            
            // Cleanup test directory
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }

        #endregion
    }
}

