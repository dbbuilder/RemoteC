using System.Text;

namespace RemoteC.Api.Services;

/// <summary>
/// File transfer service implementation
/// </summary>
public class FileTransferService : IFileTransferService
{
    private readonly ILogger<FileTransferService> _logger;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<Guid, FileTransferStatus> _activeTransfers;

    public FileTransferService(
        ILogger<FileTransferService> logger,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _activeTransfers = new Dictionary<Guid, FileTransferStatus>();
    }

    public async Task<FileTransferResult> StartFileTransferAsync(Guid sessionId, FileTransferRequest request)
    {
        try
        {
            _logger.Information("Starting file transfer for session {SessionId}: {FileName}", sessionId, request.FileName);

            // Validate file size
            var maxFileSize = _configuration.GetValue<long>("FileTransfer:MaxFileSizeBytes", 1024 * 1024 * 1024); // 1GB default
            if (request.FileSize > maxFileSize)
            {
                throw new ArgumentException($"File size {request.FileSize} exceeds maximum allowed size {maxFileSize}");
            }

            // Validate file extension
            var allowedExtensions = _configuration.GetSection("FileTransfer:AllowedExtensions").Get<string[]>() ?? Array.Empty<string>();
            var fileExtension = Path.GetExtension(request.FileName).ToLowerInvariant();
            
            if (allowedExtensions.Length > 0 && !allowedExtensions.Contains(fileExtension))
            {
                throw new ArgumentException($"File extension {fileExtension} is not allowed");
            }

            var transferId = Guid.NewGuid();
            var transferStatus = new FileTransferStatus
            {
                TransferId = transferId,
                State = FileTransferState.Queued,
                BytesTransferred = 0,
                TotalBytes = request.FileSize,
                ProgressPercentage = 0
            };

            _activeTransfers[transferId] = transferStatus;

            // Start transfer in background
            _ = Task.Run(async () => await ProcessFileTransferAsync(transferId, sessionId, request));

            return new FileTransferResult
            {
                TransferId = transferId,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error starting file transfer for session {SessionId}", sessionId);
            return new FileTransferResult
            {
                TransferId = Guid.Empty,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<FileTransferStatus> GetTransferStatusAsync(Guid transferId)
    {
        await Task.CompletedTask;
        
        if (_activeTransfers.TryGetValue(transferId, out var status))
        {
            return status;
        }

        throw new ArgumentException($"Transfer {transferId} not found");
    }

    public async Task CancelTransferAsync(Guid transferId)
    {
        try
        {
            _logger.Information("Cancelling file transfer {TransferId}", transferId);

            if (_activeTransfers.TryGetValue(transferId, out var status))
            {
                status.State = FileTransferState.Cancelled;
                _logger.Information("File transfer {TransferId} cancelled", transferId);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error cancelling transfer {TransferId}", transferId);
            throw;
        }
    }

    public async Task<IEnumerable<FileTransferHistoryDto>> GetTransferHistoryAsync(Guid sessionId)
    {
        // TODO: Implement transfer history storage and retrieval
        await Task.CompletedTask;
        return new List<FileTransferHistoryDto>();
    }

    private async Task ProcessFileTransferAsync(Guid transferId, Guid sessionId, FileTransferRequest request)
    {
        try
        {
            var status = _activeTransfers[transferId];
            status.State = FileTransferState.InProgress;

            // Simulate file transfer progress
            var chunkSize = 64 * 1024; // 64KB chunks
            var totalChunks = (int)Math.Ceiling((double)request.FileSize / chunkSize);

            for (int i = 0; i < totalChunks; i++)
            {
                if (status.State == FileTransferState.Cancelled)
                    break;

                // Simulate processing time
                await Task.Delay(100);

                var bytesInChunk = Math.Min(chunkSize, (int)(request.FileSize - status.BytesTransferred));
                status.BytesTransferred += bytesInChunk;
                status.ProgressPercentage = (double)status.BytesTransferred / request.FileSize * 100;

                var remainingBytes = request.FileSize - status.BytesTransferred;
                var estimatedTimeRemaining = TimeSpan.FromMilliseconds(remainingBytes / chunkSize * 100);
                status.EstimatedTimeRemaining = estimatedTimeRemaining;
            }

            if (status.State != FileTransferState.Cancelled)
            {
                status.State = FileTransferState.Completed;
                status.ProgressPercentage = 100;
                status.EstimatedTimeRemaining = TimeSpan.Zero;
            }

            _logger.Information("File transfer {TransferId} completed with state {State}", transferId, status.State);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing file transfer {TransferId}", transferId);
            
            if (_activeTransfers.TryGetValue(transferId, out var status))
            {
                status.State = FileTransferState.Failed;
                status.ErrorMessage = ex.Message;
            }
        }
    }
}