using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RemoteC.Shared.Models;

namespace RemoteC.Client.Services
{
    public interface IFileTransferService
    {
        event EventHandler<FileTransferProgressEventArgs>? TransferProgressChanged;
        event EventHandler<FileTransferCompletedEventArgs>? TransferCompleted;

        Task<Guid> StartUploadAsync(Guid sessionId, string localPath);
        Task<Guid> StartDownloadAsync(Guid sessionId, string remotePath, string localPath);
        Task PauseTransferAsync(Guid transferId);
        Task ResumeTransferAsync(Guid transferId);
        Task CancelTransferAsync(Guid transferId);
        Task<FileTransferStatus> GetTransferStatusAsync(Guid transferId);
        Task<List<FileTransferStatus>> GetActiveTransfersAsync();
    }

    public class FileTransferProgressEventArgs : EventArgs
    {
        public Guid TransferId { get; set; }
        public int ProgressPercentage { get; set; }
        public long BytesTransferred { get; set; }
        public long TotalBytes { get; set; }
        public double TransferSpeed { get; set; } // Bytes per second
        public TimeSpan EstimatedTimeRemaining { get; set; }
    }

    public class FileTransferCompletedEventArgs : EventArgs
    {
        public Guid TransferId { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class FileTransferStatus
    {
        public Guid TransferId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public TransferDirection Direction { get; set; }
        public TransferStatus Status { get; set; }
        public int ProgressPercentage { get; set; }
        public long BytesTransferred { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? CompletedTime { get; set; }
    }
}