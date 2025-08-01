using System;
using System.Collections.Generic;

namespace RemoteC.Shared.Models
{
    /// <summary>
    /// Represents clipboard content that can be synchronized
    /// </summary>
    public class ClipboardContent
    {
        /// <summary>
        /// Type of clipboard content
        /// </summary>
        public ClipboardContentType Type { get; set; }

        /// <summary>
        /// Text content (for Text, Html, RichText types)
        /// </summary>
        public string? Text { get; set; }

        /// <summary>
        /// HTML content
        /// </summary>
        public string? Html { get; set; }

        /// <summary>
        /// Rich text content (RTF)
        /// </summary>
        public string? RichText { get; set; }

        /// <summary>
        /// Image data bytes
        /// </summary>
        public byte[]? ImageData { get; set; }

        /// <summary>
        /// Image format (PNG, JPEG, BMP, etc.)
        /// </summary>
        public string? ImageFormat { get; set; }

        /// <summary>
        /// List of files for file drop operations
        /// </summary>
        public ClipboardFile[]? Files { get; set; }

        /// <summary>
        /// Size of content in bytes
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Timestamp when content was captured
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether content is compressed
        /// </summary>
        public bool IsCompressed { get; set; }

        /// <summary>
        /// Compressed data if IsCompressed is true
        /// </summary>
        public byte[]? CompressedData { get; set; }

        /// <summary>
        /// Whether content was truncated due to size limits
        /// </summary>
        public bool IsTruncated { get; set; }

        /// <summary>
        /// Hash of content for duplicate detection
        /// </summary>
        public string? ContentHash { get; set; }

        /// <summary>
        /// Source that resolved this content (for conflict resolution)
        /// </summary>
        public ClipboardSource? ResolvedSource { get; set; }
    }

    /// <summary>
    /// Types of clipboard content
    /// </summary>
    public enum ClipboardContentType
    {
        /// <summary>
        /// Plain text
        /// </summary>
        Text,

        /// <summary>
        /// HTML formatted text
        /// </summary>
        Html,

        /// <summary>
        /// Rich text format (RTF)
        /// </summary>
        RichText,

        /// <summary>
        /// Image data
        /// </summary>
        Image,

        /// <summary>
        /// List of file paths
        /// </summary>
        FileList,

        /// <summary>
        /// Custom application data
        /// </summary>
        Custom
    }

    /// <summary>
    /// Represents a file in clipboard
    /// </summary>
    public class ClipboardFile
    {
        /// <summary>
        /// Full path to the file
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// File name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Whether file exists and is accessible
        /// </summary>
        public bool IsAccessible { get; set; }
    }

    /// <summary>
    /// Direction of clipboard synchronization
    /// </summary>
    public enum ClipboardSyncDirection
    {
        /// <summary>
        /// No synchronization
        /// </summary>
        None,

        /// <summary>
        /// Host to client only
        /// </summary>
        HostToClient,

        /// <summary>
        /// Client to host only
        /// </summary>
        ClientToHost,

        /// <summary>
        /// Bidirectional sync
        /// </summary>
        Bidirectional
    }

    /// <summary>
    /// Source of clipboard content
    /// </summary>
    public enum ClipboardSource
    {
        /// <summary>
        /// Content from host machine
        /// </summary>
        Host,

        /// <summary>
        /// Content from client machine
        /// </summary>
        Client
    }

    /// <summary>
    /// Target for clipboard operations
    /// </summary>
    public enum ClipboardTarget
    {
        /// <summary>
        /// Host clipboard
        /// </summary>
        Host,

        /// <summary>
        /// Client clipboard
        /// </summary>
        Client,

        /// <summary>
        /// Both clipboards
        /// </summary>
        Both
    }

    /// <summary>
    /// Clipboard synchronization result
    /// </summary>
    public class ClipboardSyncResult
    {
        /// <summary>
        /// Whether sync was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Content that was synchronized
        /// </summary>
        public ClipboardContent? SyncedContent { get; set; }

        /// <summary>
        /// Actual direction of sync that occurred
        /// </summary>
        public ClipboardSyncDirection ActualDirection { get; set; }

        /// <summary>
        /// Whether a conflict was resolved
        /// </summary>
        public bool ConflictResolved { get; set; }

        /// <summary>
        /// Timestamp of sync
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Configuration for clipboard monitoring
    /// </summary>
    public class ClipboardMonitoringConfig
    {
        /// <summary>
        /// Whether monitoring is enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Polling interval in milliseconds
        /// </summary>
        public int PollingIntervalMs { get; set; } = 1000;

        /// <summary>
        /// Sync direction
        /// </summary>
        public ClipboardSyncDirection Direction { get; set; } = ClipboardSyncDirection.Bidirectional;

        /// <summary>
        /// Maximum content size in bytes
        /// </summary>
        public long MaxContentSize { get; set; } = 10 * 1024 * 1024; // 10MB

        /// <summary>
        /// Content types to monitor
        /// </summary>
        public ClipboardContentType[] AllowedTypes { get; set; } = new[]
        {
            ClipboardContentType.Text,
            ClipboardContentType.Image,
            ClipboardContentType.Html
        };

        /// <summary>
        /// Whether to compress large content
        /// </summary>
        public bool CompressLargeContent { get; set; } = true;

        /// <summary>
        /// Compression threshold in bytes
        /// </summary>
        public long CompressionThreshold { get; set; } = 100 * 1024; // 100KB
    }

    /// <summary>
    /// Clipboard history item
    /// </summary>
    public class ClipboardHistoryItem
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Clipboard content
        /// </summary>
        public ClipboardContent Content { get; set; } = new();

        /// <summary>
        /// Timestamp when added to history
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Source of the content
        /// </summary>
        public ClipboardSource Source { get; set; }

        /// <summary>
        /// Session ID associated with this item
        /// </summary>
        public Guid SessionId { get; set; }
    }

    /// <summary>
    /// Policy for resolving clipboard conflicts
    /// </summary>
    public enum ConflictResolutionPolicy
    {
        /// <summary>
        /// Prefer the newest content
        /// </summary>
        PreferNewest,

        /// <summary>
        /// Prefer host content
        /// </summary>
        PreferHost,

        /// <summary>
        /// Prefer client content
        /// </summary>
        PreferClient,

        /// <summary>
        /// Manual resolution required
        /// </summary>
        Manual
    }

    /// <summary>
    /// Notification for clipboard updates
    /// </summary>
    public class ClipboardUpdateNotification
    {
        /// <summary>
        /// Session ID
        /// </summary>
        public Guid SessionId { get; set; }

        /// <summary>
        /// Source of the update
        /// </summary>
        public ClipboardSource Source { get; set; }

        /// <summary>
        /// Updated content
        /// </summary>
        public ClipboardContent Content { get; set; } = new();

        /// <summary>
        /// Timestamp of update
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Configuration for clipboard synchronization
    /// </summary>
    public class ClipboardSyncConfig
    {
        /// <summary>
        /// Whether sync is enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Sync direction
        /// </summary>
        public ClipboardSyncDirection Direction { get; set; }

        /// <summary>
        /// Sync interval in milliseconds
        /// </summary>
        public int IntervalMs { get; set; } = 1000;

        /// <summary>
        /// Maximum retry attempts
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Retry delay in milliseconds
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;

        /// <summary>
        /// Conflict resolution policy
        /// </summary>
        public ConflictResolutionPolicy ConflictPolicy { get; set; } = ConflictResolutionPolicy.PreferNewest;
    }
}