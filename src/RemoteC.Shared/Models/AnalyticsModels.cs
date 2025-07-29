using System;
using System.Collections.Generic;

namespace RemoteC.Shared.Models
{
    // Analytics Options
    public class AnalyticsOptions
    {
        public bool EnableRealTimeAnalytics { get; set; }
        public int DataRetentionDays { get; set; }
        public int AggregationIntervalMinutes { get; set; }
        public bool EnablePredictiveAnalytics { get; set; }
        public Dictionary<string, double> AlertThresholds { get; set; } = new();
    }

    // Session Analytics
    public class SessionAnalytics
    {
        public int TotalSessions { get; set; }
        public int UniqueSessions { get; set; }
        public int UniqueUsers { get; set; }
        public TimeSpan AverageSessionDuration { get; set; }
        public TimeSpan TotalSessionTime { get; set; }
        public Dictionary<DateTime, int> SessionsByDay { get; set; } = new();
        public Dictionary<int, int> SessionsByHour { get; set; } = new();
        public Dictionary<string, int> SessionsByLocation { get; set; } = new();
        public Dictionary<string, int> SessionsByDevice { get; set; } = new();
        public double CompletionRate { get; set; }
    }

    public class RealTimeSessionMetrics
    {
        public int ActiveSessions { get; set; }
        public double SessionsPerMinute { get; set; }
        public Dictionary<string, int> ActiveUsersByLocation { get; set; } = new();
        public Dictionary<string, int> DeviceTypes { get; set; } = new();
        public DateTime Timestamp { get; set; }
        public int ConcurrentUsers { get; set; }
        public double AverageLatency { get; set; }
    }

    public class SessionTrends
    {
        public double GrowthRate { get; set; }
        public List<string> PeakHours { get; set; } = new();
        public List<string> PeakDays { get; set; } = new();
        public int PredictedNextWeekSessions { get; set; }
        public Dictionary<DateTime, int> HistoricalTrend { get; set; } = new();
        public string TrendDirection { get; set; } = string.Empty;
        public double SeasonalityIndex { get; set; }
    }

    // Performance Analytics
    public class PerformanceMetrics
    {
        public double AverageLatency { get; set; }
        public double P50Latency { get; set; }
        public double P95Latency { get; set; }
        public double P99Latency { get; set; }
        public double Throughput { get; set; }
        public double ErrorRate { get; set; }
        public double SuccessRate { get; set; }
        public Dictionary<int, int> LatencyHistogram { get; set; } = new();
        public Dictionary<string, double> EndpointMetrics { get; set; } = new();
        public DateTime MeasurementPeriodStart { get; set; }
        public DateTime MeasurementPeriodEnd { get; set; }
    }

    public class ResourceUtilization
    {
        public string ServerId { get; set; } = string.Empty;
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double DiskUsage { get; set; }
        public double NetworkBandwidth { get; set; }
        public double NetworkIn { get; set; }
        public double NetworkOut { get; set; }
        public Dictionary<string, ProcessMetric> ProcessMetrics { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }

    public class ProcessMetric
    {
        public string ProcessName { get; set; } = string.Empty;
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
    }

    public class PerformanceAnomaly
    {
        public Guid Id { get; set; }
        public AnomalyType Type { get; set; }
        public AnomalySeverity Severity { get; set; }
        public string MetricName { get; set; } = string.Empty;
        public double ExpectedValue { get; set; }
        public double ActualValue { get; set; }
        public double Confidence { get; set; }
        public DateTime DetectedAt { get; set; }
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object> Context { get; set; } = new();
    }

    public enum AnomalyType
    {
        LatencySpike,
        ErrorRateIncrease,
        TrafficDrop,
        UnusualActivity,
        ResourceExhaustion,
        SecurityThreat
    }

    public enum AnomalySeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    // User Analytics
    public class UserBehaviorAnalytics
    {
        public List<UserActivity> MostActiveUsers { get; set; } = new();
        public Dictionary<string, int> CommonActions { get; set; } = new();
        public List<UserJourney> UserJourneys { get; set; } = new();
        public double AverageActionsPerSession { get; set; }
        public double BounceRate { get; set; }
        public Dictionary<string, double> FeatureAdoption { get; set; } = new();
        public Dictionary<int, int> ActivityByHour { get; set; } = new();
    }

    public class UserActivity
    {
        public Guid UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public int ActionCount { get; set; }
        public DateTime LastActivity { get; set; }
        public TimeSpan TotalActiveTime { get; set; }
    }

    public class UserJourney
    {
        public string JourneyName { get; set; } = string.Empty;
        public List<string> Steps { get; set; } = new();
        public int UserCount { get; set; }
        public double CompletionRate { get; set; }
        public TimeSpan AverageDuration { get; set; }
    }

    public class UserEngagementMetrics
    {
        public double DailyActiveRate { get; set; }
        public double WeeklyActiveRate { get; set; }
        public double MonthlyActiveRate { get; set; }
        public double RetentionRate { get; set; }
        public int SessionFrequency { get; set; }
        public Dictionary<DateTime, double> EngagementTrend { get; set; } = new();
        public double Stickiness { get; set; } // DAU/MAU ratio
        public int ConsecutiveDaysActive { get; set; }
    }

    public class ChurnPrediction
    {
        public Guid UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public double ChurnProbability { get; set; }
        public List<string> RiskFactors { get; set; } = new();
        public DateTime PredictionDate { get; set; }
        public string RecommendedAction { get; set; } = string.Empty;
        public int DaysSinceLastActivity { get; set; }
        public ChurnRiskLevel RiskLevel { get; set; }
    }

    public enum ChurnRiskLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    // Business Analytics
    public class BusinessMetrics
    {
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRecurringRevenue { get; set; }
        public decimal AverageRevenuePerUser { get; set; }
        public decimal CustomerAcquisitionCost { get; set; }
        public decimal LifetimeValue { get; set; }
        public double ChurnRate { get; set; }
        public int TotalCustomers { get; set; }
        public int NewCustomers { get; set; }
        public Dictionary<string, decimal> RevenueByProduct { get; set; } = new();
        public Dictionary<DateTime, decimal> RevenueGrowth { get; set; } = new();
    }

    public class ConversionFunnel
    {
        public List<FunnelStage> Stages { get; set; } = new();
        public double OverallConversionRate { get; set; }
        public Dictionary<string, double> StageDropoffRates { get; set; } = new();
        public TimeSpan AverageTimeToConversion { get; set; }
        public Dictionary<string, double> ConversionBySource { get; set; } = new();
    }

    public class FunnelStage
    {
        public string Name { get; set; } = string.Empty;
        public int Users { get; set; }
        public double ConversionRate { get; set; }
        public TimeSpan AverageTimeInStage { get; set; }
        public int ExitCount { get; set; }
    }

    // Alerts and Monitoring
    public class Alert
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MetricName { get; set; } = string.Empty;
        public double CurrentValue { get; set; }
        public double ThresholdValue { get; set; }
        public AlertSeverity Severity { get; set; }
        public DateTime TriggeredAt { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, string> Tags { get; set; } = new();
        public bool IsAcknowledged { get; set; }
    }

    public class CustomAlert
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string MetricName { get; set; } = string.Empty;
        public AlertCondition Condition { get; set; }
        public double Threshold { get; set; }
        public TimeSpan Duration { get; set; }
        public AlertSeverity Severity { get; set; }
        public bool IsActive { get; set; }
        public List<string> NotificationChannels { get; set; } = new();
        public Dictionary<string, string> Tags { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? LastTriggered { get; set; }
    }

    public enum AlertSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    public enum AlertCondition
    {
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
        Equal,
        NotEqual,
        Contains,
        NotContains
    }

    // Dashboards and Reporting
    public class ExecutiveDashboard
    {
        public Dictionary<string, object> KeyMetrics { get; set; } = new();
        public List<ChartData> Charts { get; set; } = new();
        public List<string> Insights { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public Dictionary<string, double> Comparisons { get; set; } = new(); // vs previous period
    }

    public class ChartData
    {
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // line, bar, pie, etc.
        public List<DataPoint> DataPoints { get; set; } = new();
        public Dictionary<string, string> Options { get; set; } = new();
    }

    public class DataPoint
    {
        public string Label { get; set; } = string.Empty;
        public double Value { get; set; }
        public DateTime? Timestamp { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ScheduledReport
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ReportType Type { get; set; }
        public ReportSchedule Schedule { get; set; }
        public string[] Recipients { get; set; } = Array.Empty<string>();
        public Guid OrganizationId { get; set; }
        public bool IsActive { get; set; }
        public DateTime? NextRunTime { get; set; }
        public DateTime? LastRunTime { get; set; }
        public Dictionary<string, string> Parameters { get; set; } = new();
    }

    public class CustomReport
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public ReportFormat Format { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public List<string> Sections { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class CustomReportRequest
    {
        public Guid OrganizationId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string[] Metrics { get; set; } = Array.Empty<string>();
        public ReportFormat Format { get; set; }
        public bool IncludeCharts { get; set; }
        public bool IncludeRawData { get; set; }
        public Dictionary<string, string> Filters { get; set; } = new();
    }

    public enum ReportType
    {
        Performance,
        Usage,
        Security,
        Executive,
        Compliance,
        Custom
    }

    public enum ReportSchedule
    {
        Daily,
        Weekly,
        BiWeekly,
        Monthly,
        Quarterly,
        Yearly
    }

    public enum ReportFormat
    {
        Pdf,
        Excel,
        Html,
        Json,
        Csv
    }

    // Data Export
    public class DataExport
    {
        public Guid Id { get; set; }
        public Guid RecordingId { get; set; } // Added for test compatibility
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public ExportFormat Format { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public ExportStatus Status { get; set; }
        public int RecordCount { get; set; }
    }

    public class DataExportRequest
    {
        public Guid OrganizationId { get; set; }
        public string[] DataTypes { get; set; } = Array.Empty<string>();
        public ExportFormat Format { get; set; }
        public DateRange DateRange { get; set; } = null!;
        public Dictionary<string, string> Filters { get; set; } = new();
        public bool CompressOutput { get; set; }
    }

    public class DateRange
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public DateRange(DateTime startDate, DateTime endDate)
        {
            StartDate = startDate;
            EndDate = endDate;
        }
    }

    public enum ExportFormat
    {
        Json,
        Csv,
        Excel,
        Parquet,
        MP4,  // Added for test compatibility
        Xml,
        Pdf
    }

    public enum ExportStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Expired
    }
}