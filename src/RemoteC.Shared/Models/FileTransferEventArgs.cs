using System;

namespace RemoteC.Shared.Models
{
    /// <summary>
    /// Event args for file transfer progress updates
    /// </summary>
    public class FileTransferProgressEventArgs : EventArgs
    {
        public Guid TransferId { get; set; }
        public long BytesTransferred { get; set; }
        public long TotalBytes { get; set; }
        public int ProgressPercentage { get; set; }
        public double TransferRate { get; set; } // Bytes per second
        public TimeSpan EstimatedTimeRemaining { get; set; }
        public string? CurrentFile { get; set; }
    }

    /// <summary>
    /// Event args for file transfer completion
    /// </summary>
    public class FileTransferCompletedEventArgs : EventArgs
    {
        public Guid TransferId { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public long TotalBytesTransferred { get; set; }
        public TimeSpan Duration { get; set; }
        public double AverageTransferRate { get; set; } // Bytes per second
    }

    /// <summary>
    /// Simplified event args for file transfer progress (compatibility)
    /// </summary>
    public class TransferProgressEventArgs : FileTransferProgressEventArgs
    {
    }

    /// <summary>
    /// Simplified event args for file transfer completion (compatibility)
    /// </summary>
    public class TransferCompletedEventArgs : FileTransferCompletedEventArgs
    {
    }
}