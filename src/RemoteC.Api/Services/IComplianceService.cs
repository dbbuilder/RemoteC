using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RemoteC.Shared.Models;
using ConsentRecord = RemoteC.Shared.Models.ConsentRecord;
using PHIAccessLog = RemoteC.Shared.Models.PHIAccessLog;
using RetentionPolicy = RemoteC.Shared.Models.RetentionPolicy;

namespace RemoteC.Api.Services
{
    public interface IComplianceService
    {
        // SOC2 Compliance
        Task<ComplianceValidationResult> ValidateSOC2ComplianceAsync(Guid organizationId);
        Task<List<ComplianceControl>> GetSOC2ControlsAsync();
        Task<SOC2Report> GenerateSOC2ReportAsync(Guid organizationId, DateTime startDate, DateTime endDate);
        
        // GDPR Compliance
        Task<GDPRComplianceResult> ValidateGDPRComplianceAsync(Guid organizationId);
        Task<DataSubjectResponse> ProcessDataSubjectRequestAsync(DataSubjectRequest request);
        Task<List<ConsentRecord>> GetConsentRecordsAsync(Guid userId);
        Task<ConsentRecord> UpdateConsentAsync(ConsentUpdate update);
        
        // HIPAA Compliance
        Task<ComplianceValidationResult> ValidateHIPAAComplianceAsync(Guid organizationId);
        Task<PHIAccessLog> LogPHIAccessAsync(PHIAccessRequest request);
        Task<List<PHIAccessLog>> GetPHIAccessLogsAsync(Guid? patientId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<BreachNotificationResult> ReportBreachAsync(BreachNotification notification);
        
        // Data Retention
        Task<RetentionPolicy> GetRetentionPolicyAsync(string dataType);
        Task<int> ApplyRetentionPoliciesAsync();
        
        // Export and Reporting
        Task<ComplianceReport> GenerateComplianceReportAsync(ComplianceReportRequest request);
        
        // Monitoring and Dashboard - Added for test compatibility
        Task<List<ComplianceViolation>> MonitorComplianceAsync(Guid organizationId);
        Task<ComplianceDashboard> GenerateComplianceDashboardAsync(Guid organizationId);
    }
}