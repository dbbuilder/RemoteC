using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
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
    public class SessionRecordingServiceTests : IDisposable
    {
        private readonly RemoteCDbContext _context;
        private readonly Mock<BlobServiceClient> _blobServiceClientMock;
        private readonly Mock<IEncryptionService> _encryptionServiceMock;
        private readonly Mock<IAuditService> _auditServiceMock;
        private readonly Mock<ILogger<SessionRecordingService>> _loggerMock;
        private readonly SessionRecordingService _service;
        private readonly RemoteC.Shared.Models.SessionRecordingOptions _options;

        public SessionRecordingServiceTests()
        {
            var options = new DbContextOptionsBuilder<RemoteCDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new RemoteCDbContext(options);

            _blobServiceClientMock = new Mock<BlobServiceClient>();
            _encryptionServiceMock = new Mock<IEncryptionService>();
            _auditServiceMock = new Mock<IAuditService>();
            _loggerMock = new Mock<ILogger<SessionRecordingService>>();

            _options = new RemoteC.Shared.Models.SessionRecordingOptions
            {
                StorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=test;",
                MaxRecordingDuration = TimeSpan.FromHours(2),
                ChunkSize = 4 * 1024 * 1024,
                DefaultCompressionType = CompressionType.Gzip,
                DefaultQuality = RecordingQuality.High,
                DefaultFrameRate = 30
            };

            _service = new SessionRecordingService(
                _context,
                _blobServiceClientMock.Object,
                _encryptionServiceMock.Object,
                _auditServiceMock.Object,
                Options.Create(_options),
                _loggerMock.Object);

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Test Organization"
            };

            var orgSettings = new OrganizationSettings
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                SessionRecordingEnabled = true,
                SessionRecordingRetentionDays = 30
            };

            _context.Organizations.Add(organization);
            _context.OrganizationSettings.Add(orgSettings);
            _context.SaveChanges();
        }

        #region StartRecordingAsync Tests

        [Fact]
        public async Task StartRecordingAsync_WithValidParams_CreatesRecording()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var organizationId = _context.Organizations.First().Id;
            var recordingOptions = new RemoteC.Api.Services.RecordingOptions
            {
                CompressionType = CompressionType.Gzip,
                IncludeAudio = true,
                Quality = RecordingQuality.High,
                FrameRate = 30
            };

            var encryptionKeyId = "test-key-id";
            _encryptionServiceMock.Setup(e => e.GenerateKeyAsync())
                .ReturnsAsync(encryptionKeyId);

            var containerClientMock = new Mock<BlobContainerClient>();
            _blobServiceClientMock.Setup(b => b.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(containerClientMock.Object);

            // Act
            var result = await _service.StartRecordingAsync(sessionId, organizationId, recordingOptions);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(sessionId, result.SessionId);
            Assert.Equal(organizationId, result.OrganizationId);
            Assert.Equal(RemoteC.Data.Entities.RecordingStatus.Recording, result.Status);
            Assert.Equal(encryptionKeyId, result.EncryptionKeyId);
            Assert.True(result.IncludeAudio);
            Assert.Equal(RecordingQuality.High, result.Quality);
            Assert.Equal(30, result.FrameRate);

            // Verify database
            var dbRecording = await _context.SessionRecordings.FirstOrDefaultAsync(r => r.Id == result.Id);
            Assert.NotNull(dbRecording);

            // Verify audit
            _auditServiceMock.Verify(a => a.LogAsync(It.IsAny<AuditLogEntry>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task StartRecordingAsync_WhenRecordingDisabled_ThrowsException()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var organizationId = Guid.NewGuid(); // Non-existent org

            var recordingOptions = new RemoteC.Api.Services.RecordingOptions();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.StartRecordingAsync(sessionId, organizationId, recordingOptions));
        }

        #endregion

        #region StopRecordingAsync Tests

        [Fact]
        public async Task StopRecordingAsync_WithActiveRecording_StopsRecording()
        {
            // Arrange
            var recording = new RemoteC.Data.Entities.SessionRecording
            {
                Id = Guid.NewGuid(),
                SessionId = Guid.NewGuid(),
                OrganizationId = _context.Organizations.First().Id,
                StartedAt = DateTime.UtcNow.AddMinutes(-30),
                Status = RemoteC.Data.Entities.RecordingStatus.Recording,
                EncryptionKeyId = "test-key",
                FileSize = 1024 * 1024 * 50 // 50MB
            };
            _context.SessionRecordings.Add(recording);
            await _context.SaveChangesAsync();

            // Act
            await _service.StopRecordingAsync(recording.Id);

            // Assert
            var dbRecording = await _context.SessionRecordings.FindAsync(recording.Id);
            Assert.NotNull(dbRecording);
            Assert.Equal(RemoteC.Data.Entities.RecordingStatus.Completed, dbRecording.Status);
            Assert.NotNull(dbRecording.EndedAt);
            Assert.True(dbRecording.Duration > TimeSpan.Zero);

            _auditServiceMock.Verify(a => a.LogAsync(It.IsAny<AuditLogEntry>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region AppendFrameAsync Tests

        [Fact]
        public async Task AppendFrameAsync_WithValidFrame_AppendsSuccessfully()
        {
            // Arrange
            var recording = new RemoteC.Data.Entities.SessionRecording
            {
                Id = Guid.NewGuid(),
                SessionId = Guid.NewGuid(),
                OrganizationId = _context.Organizations.First().Id,
                StartedAt = DateTime.UtcNow,
                Status = RemoteC.Data.Entities.RecordingStatus.Recording,
                EncryptionKeyId = "test-key",
                CompressionType = CompressionType.None
            };
            _context.SessionRecordings.Add(recording);
            await _context.SaveChangesAsync();

            var frame = new RemoteC.Api.Services.RecordingFrame
            {
                FrameNumber = 1,
                Timestamp = DateTime.UtcNow,
                Data = new byte[] { 1, 2, 3, 4, 5 },
                IsKeyFrame = true,
                Width = 1920,
                Height = 1080
            };

            var encryptedData = new byte[] { 10, 20, 30, 40, 50 };
            _encryptionServiceMock.Setup(e => e.EncryptAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(encryptedData);

            var blobClientMock = new Mock<BlobClient>();
            var containerClientMock = new Mock<BlobContainerClient>();
            containerClientMock.Setup(c => c.GetBlobClient(It.IsAny<string>()))
                .Returns(blobClientMock.Object);
            _blobServiceClientMock.Setup(b => b.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(containerClientMock.Object);

            // Act
            await _service.AppendFrameAsync(recording.Id, frame);

            // Assert
            var dbRecording = await _context.SessionRecordings.FindAsync(recording.Id);
            Assert.NotNull(dbRecording);
            Assert.True(dbRecording.FileSizeBytes > 0);
            Assert.True(dbRecording.FrameCount > 0);

            _encryptionServiceMock.Verify(e => e.EncryptAsync(frame.Data, recording.EncryptionKeyId), Times.Once);
        }

        #endregion

        #region GetRecordingAsync Tests

        [Fact]
        public async Task GetRecordingAsync_WithValidId_ReturnsRecording()
        {
            // Arrange
            var recording = new RemoteC.Data.Entities.SessionRecording
            {
                Id = Guid.NewGuid(),
                SessionId = Guid.NewGuid(),
                OrganizationId = _context.Organizations.First().Id,
                StartedAt = DateTime.UtcNow.AddHours(-1),
                EndedAt = DateTime.UtcNow,
                Status = RemoteC.Data.Entities.RecordingStatus.Completed,
                EncryptionKeyId = "test-key",
                FileSize = 1024 * 1024 * 100,
                Duration = TimeSpan.FromHours(1),
                FrameCount = 108000
            };
            _context.SessionRecordings.Add(recording);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetRecordingAsync(recording.Id, Guid.NewGuid());

            // Assert
            Assert.NotNull(result);
            Assert.Equal(recording.Id, result.RecordingId);
            Assert.Equal(recording.SessionId, result.SessionId);
            Assert.Equal(recording.Duration, result.Duration);
        }

        #endregion

        #region ExportRecordingAsync Tests

        [Fact]
        public async Task ExportRecordingAsync_WithValidRecording_ExportsSuccessfully()
        {
            // Arrange
            var recording = new RemoteC.Data.Entities.SessionRecording
            {
                Id = Guid.NewGuid(),
                SessionId = Guid.NewGuid(),
                OrganizationId = _context.Organizations.First().Id,
                StartedAt = DateTime.UtcNow.AddHours(-1),
                EndedAt = DateTime.UtcNow,
                Status = RemoteC.Data.Entities.RecordingStatus.Completed,
                EncryptionKeyId = "test-key",
                FileSize = 1024 * 1024,
                Duration = TimeSpan.FromHours(1)
            };
            _context.SessionRecordings.Add(recording);
            await _context.SaveChangesAsync();

            var exportOptions = new ExportOptions
            {
                Format = (RemoteC.Api.Services.RecordingExportFormat)RemoteC.Shared.Models.ExportFormat.MP4,
                Quality = (int)RemoteC.Shared.Models.ExportQuality.High,
                IncludeAudio = true
            };

            // Setup mocks
            var blobData = new byte[] { 1, 2, 3, 4, 5 };
            var decryptedData = new byte[] { 10, 20, 30, 40, 50 };

            var blobClientMock = new Mock<BlobClient>();
            blobClientMock.Setup(b => b.DownloadContentAsync())
                .ReturnsAsync(Azure.Response.FromValue(
                    BlobsModelFactory.BlobDownloadResult(BinaryData.FromBytes(blobData)), 
                    Mock.Of<Azure.Response>()));

            var containerClientMock = new Mock<BlobContainerClient>();
            containerClientMock.Setup(c => c.GetBlobClient(It.IsAny<string>()))
                .Returns(blobClientMock.Object);

            _blobServiceClientMock.Setup(b => b.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(containerClientMock.Object);

            _encryptionServiceMock.Setup(e => e.DecryptAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(decryptedData);

            // Act
            var result = await _service.ExportRecordingAsync(recording.Id, exportOptions);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(recording.Id, result.RecordingId);
            Assert.Equal(ExportStatus.Completed, result.Status);
            Assert.NotNull(result.DownloadUrl);

            _auditServiceMock.Verify(a => a.LogAsync(It.IsAny<AuditLogEntry>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}