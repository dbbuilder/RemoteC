using System;
using System.Collections.Generic;

namespace RemoteC.Shared.Models
{
    // Compliance Options
    public class ComplianceOptions
    {
        public bool EnableSOC2 { get; set; }
        public bool EnableGDPR { get; set; }
        public bool EnableHIPAA { get; set; }
        public int DataRetentionDays { get; set; }
        public bool RequireDataEncryption { get; set; }
        public bool RequireAuditLogging { get; set; }
    }

    // Compliance Validation
    public class ComplianceValidationResult
    {
        public string Framework { get; set; } = string.Empty;
        public bool IsCompliant { get; set; }
        public DateTime ValidatedAt { get; set; }
        public List<ComplianceViolation> Violations { get; set; } = new();
        public List<ComplianceControl> Controls { get; set; } = new();
    }

    public class ComplianceViolation
    {
        public string ControlId { get; set; } = string.Empty;
        public string ControlName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ViolationSeverity Severity { get; set; }
        public string[] RemediationSteps { get; set; } = Array.Empty<string>();
        public string Requirement { get; set; } = string.Empty; // Added for compatibility
        public string Impact { get; set; } = string.Empty; // Added for compatibility
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow; // Added for compatibility
    }

    public class ComplianceControl
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public ControlStatus Status { get; set; }
        public string[] RemediationSteps { get; set; } = Array.Empty<string>();
    }

    public enum ViolationSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum ControlStatus
    {
        NotAssessed,
        Satisfied,
        NotSatisfied,
        PartiallyImplemented
    }

    // SOC2 Models
    public class SOC2Report
    {
        public Guid OrganizationId { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public DateTime GeneratedAt { get; set; }
        public List<ControlAssessment> ControlAssessments { get; set; } = new();
        public string ExecutiveSummary { get; set; } = string.Empty;
        public string AuditorStatement { get; set; } = string.Empty;
    }

    public class ControlAssessment
    {
        public string ControlId { get; set; } = string.Empty;
        public string ControlName { get; set; } = string.Empty;
        public ControlStatus Status { get; set; }
        public string[] TestingProcedures { get; set; } = Array.Empty<string>();
        public string TestResults { get; set; } = string.Empty;
        public int IncidentCount { get; set; }
        public string RemediationStatus { get; set; } = string.Empty;
    }

    public class ControlIncident
    {
        public Guid Id { get; set; }
        public string ControlId { get; set; } = string.Empty;
        public DateTime IncidentDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Resolution { get; set; } = string.Empty;
    }

    // GDPR Models
    public class GDPRComplianceResult
    {
        public bool IsCompliant { get; set; }
        public List<ComplianceViolation> Violations { get; set; } = new();
        public bool HasPrivacyPolicy { get; set; }
        public bool HasDataProcessingAgreements { get; set; }
        public bool HasConsentMechanism { get; set; }
        public bool HasDataProtectionOfficer { get; set; }
        public DateTime LastAssessmentDate { get; set; }
    }

    public class DataSubjectRequest
    {
        public Guid UserId { get; set; }
        public DataSubjectRequestType RequestType { get; set; }
        public string RequestedBy { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public ExportFormat ExportFormat { get; set; } = ExportFormat.Json;
        public Dictionary<string, string> AdditionalData { get; set; } = new();
    }

    public class DataSubjectResponse
    {
        public Guid RequestId { get; set; }
        public DataSubjectRequestStatus Status { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
        public byte[]? ExportedData { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public enum DataSubjectRequestType
    {
        Access,
        Erasure,
        Portability,
        Rectification,
        Restriction
    }

    public enum DataSubjectRequestStatus
    {
        Pending,
        Processing,
        Completed,
        Failed
    }

    public class ConsentUpdate
    {
        public Guid UserId { get; set; }
        public Guid OrganizationId { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public bool Granted { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
    }

    // HIPAA Models
    public class PHIAccessRequest
    {
        public Guid UserId { get; set; }
        public Guid PatientId { get; set; }
        public string AccessType { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string DataAccessed { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
    }

    public class BreachNotification
    {
        public Guid OrganizationId { get; set; }
        public DateTime DiscoveryDate { get; set; }
        public DateTime IncidentDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> DataTypesInvolved { get; set; } = new();
        public int AffectedIndividuals { get; set; }
        public string ReportedBy { get; set; } = string.Empty;
    }

    public class BreachNotificationResult
    {
        public Guid BreachId { get; set; }
        public bool RequiresIndividualNotification { get; set; }
        public bool RequiresMediaNotification { get; set; }
        public bool RequiresHHSNotification { get; set; }
        public DateTime NotificationDeadline { get; set; }
        public List<string> RecommendedActions { get; set; } = new();
    }

    // Reporting Models
    public class ComplianceReportRequest
    {
        public Guid OrganizationId { get; set; }
        public bool IncludeSOC2 { get; set; }
        public bool IncludeGDPR { get; set; }
        public bool IncludeHIPAA { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ExportFormat Format { get; set; } = ExportFormat.Json;
        public string RequestedBy { get; set; } = string.Empty;
    }

    public class ComplianceReport
    {
        public Guid OrganizationId { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
        public Dictionary<string, ComplianceValidationResult> Frameworks { get; set; } = new();
        public bool OverallCompliant { get; set; }
        public int TotalViolations { get; set; }
        public int CriticalViolations { get; set; }
        public byte[]? ExportedData { get; set; }
    }

    // Added for test compatibility
    public class ComplianceDashboard
    {
        public Guid OrganizationId { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, ComplianceValidationResult> FrameworkStatus { get; set; } = new();
        public List<ComplianceViolation> RecentViolations { get; set; } = new();
        public Dictionary<string, int> ViolationsByFramework { get; set; } = new();
        public Dictionary<string, double> ComplianceScores { get; set; } = new();
        public int TotalControls { get; set; }
        public int PassingControls { get; set; }
        public int FailingControls { get; set; }
        public double OverallComplianceScore { get; set; }
        public DateTime? LastAuditDate { get; set; }
        public DateTime? NextAuditDate { get; set; }
        public List<string> HighRiskAreas { get; set; } = new();
        public Dictionary<string, List<string>> ActionItems { get; set; } = new();
        
        // Additional properties for compatibility
        public List<ComplianceFrameworkStatus> ComplianceFrameworks { get; set; } = new();
        public List<AuditSchedule> UpcomingAudits { get; set; } = new();
        public double ComplianceScore { get; set; }
        public Dictionary<string, double> Trends { get; set; } = new();
        public int TotalActiveViolations { get; set; }
        public int CriticalViolations { get; set; }
    }
    
    // Support classes
    public class ComplianceFrameworkStatus
    {
        public string Framework { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? LastAssessmentDate { get; set; }
        public DateTime? NextAssessmentDate { get; set; }
        public double CompliancePercentage { get; set; }
        public int ActiveViolations { get; set; }
    }
    
    public class AuditSchedule
    {
        public Guid Id { get; set; }
        public string Framework { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; }
        public string AuditType { get; set; } = string.Empty;
        public string Auditor { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

}