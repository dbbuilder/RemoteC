using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Services
{
    public interface IAnalyticsService
    {
        // Session Analytics
        Task<SessionAnalytics> GetSessionAnalyticsAsync(Guid organizationId, DateTime startDate, DateTime endDate);
        Task<RealTimeSessionMetrics> GetRealTimeSessionMetricsAsync(Guid organizationId);
        Task<SessionTrends> GetSessionTrendsAsync(Guid organizationId, int days);
        
        // Performance Analytics
        Task<PerformanceMetrics> GetPerformanceMetricsAsync(Guid organizationId);
        Task<ResourceUtilization> GetResourceUtilizationAsync(string serverId);
        Task<List<PerformanceAnomaly>> DetectPerformanceAnomaliesAsync(Guid organizationId);
        
        // User Analytics
        Task<UserBehaviorAnalytics> GetUserBehaviorAnalyticsAsync(Guid organizationId);
        Task<UserEngagementMetrics> GetUserEngagementMetricsAsync(Guid organizationId, Guid? userId = null);
        Task<List<ChurnPrediction>> PredictUserChurnAsync(Guid organizationId);
        
        // Business Analytics
        Task<BusinessMetrics> GetBusinessMetricsAsync(Guid organizationId);
        Task<ConversionFunnel> GetConversionFunnelAsync(Guid organizationId);
        
        // Alerts and Monitoring
        Task<List<Alert>> CheckThresholdAlertsAsync();
        Task<CustomAlert> CreateCustomAlertAsync(CustomAlert alert);
        Task<bool> UpdateAlertAsync(Guid alertId, CustomAlert alert);
        Task<bool> DeleteAlertAsync(Guid alertId);
        Task<List<CustomAlert>> GetActiveAlertsAsync(Guid? organizationId = null);
        
        // Dashboards and Reporting
        Task<ExecutiveDashboard> GenerateExecutiveDashboardAsync(Guid organizationId);
        Task<ScheduledReport> ScheduleReportAsync(ScheduledReport report);
        Task<CustomReport> GenerateCustomReportAsync(CustomReportRequest request);
        
        // Data Export
        Task<DataExport> ExportAnalyticsDataAsync(DataExportRequest request);
        
        // Metrics Collection
        Task RecordMetricAsync(string metricName, double value, Dictionary<string, string>? tags = null);
        Task RecordEventAsync(string eventName, Guid organizationId, Dictionary<string, object>? properties = null);
    }
}