using System;
using System.Collections.Generic;

namespace RemoteC.Shared.Models
{
    // Edge Node Models
    public class EdgeNode
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public NodeCapacity Capacity { get; set; } = new();
        public NodeCapacity AvailableCapacity { get; set; } = new();
        public Dictionary<string, string> Labels { get; set; } = new();
        public NodeStatus Status { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime LastSeenAt { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Version { get; set; } = string.Empty;
    }

    public class EdgeNodeSpec
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public NodeCapacity Capacity { get; set; } = new();
        public Dictionary<string, string> Labels { get; set; } = new();
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; } = 2376; // Default Docker port
    }

    public class EdgeNodeUpdate
    {
        public Dictionary<string, string>? Labels { get; set; }
        public NodeCapacity? Capacity { get; set; }
        public NodeStatus? Status { get; set; }
    }

    public class EdgeNodeStatus
    {
        public Guid NodeId { get; set; }
        public NodeHealth Health { get; set; } = new();
        public bool IsOnline { get; set; }
        public int ActiveDeployments { get; set; }
        public NodeCapacity UsedCapacity { get; set; } = new();
        public DateTime LastHealthCheck { get; set; }
    }

    public class NodeCapacity
    {
        public int CPU { get; set; }
        public int MemoryGB { get; set; }
        public int StorageGB { get; set; }
        public int NetworkMbps { get; set; }
    }

    public class NodeHealth
    {
        public HealthStatus Status { get; set; }
        public double CPUUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double DiskUsage { get; set; }
        public double NetworkLatency { get; set; }
        public DateTime CheckedAt { get; set; }
        public string? Message { get; set; }
    }

    public class DecommissionResult
    {
        public bool Success { get; set; }
        public List<Guid> MigratedDeployments { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public DateTime CompletedAt { get; set; }
    }

    // Deployment Models
    public class EdgeDeployment
    {
        public Guid Id { get; set; }
        public string ApplicationName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string? PreviousVersion { get; set; }
        public Guid NodeId { get; set; }
        public DeploymentStatus Status { get; set; }
        public int Replicas { get; set; }
        public ResourceRequirements Resources { get; set; } = new();
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
        public int[] Ports { get; set; } = Array.Empty<int>();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string ContainerId { get; set; } = string.Empty;
        public LoadBalancerInfo? LoadBalancer { get; set; }
    }

    public class EdgeDeploymentRequest
    {
        public string ApplicationName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public Guid? NodeId { get; set; }
        public string? NodeLocation { get; set; }
        public ResourceRequirements Resources { get; set; } = new();
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
        public int[] Ports { get; set; } = Array.Empty<int>();
        public HealthCheckConfig? HealthCheck { get; set; }
        public AutoScalePolicy? AutoScale { get; set; }
    }

    public class DeploymentUpdate
    {
        public string? Version { get; set; }
        public Dictionary<string, string>? EnvironmentVariables { get; set; }
        public ResourceRequirements? Resources { get; set; }
        public int? Replicas { get; set; }
    }

    public class ResourceRequirements
    {
        public double CPU { get; set; }
        public double MemoryGB { get; set; }
        public int StorageGB { get; set; }
        public int? NetworkMbps { get; set; }
    }

    public class RollbackResult
    {
        public bool Success { get; set; }
        public string CurrentVersion { get; set; } = string.Empty;
        public string RolledBackFrom { get; set; } = string.Empty;
        public DateTime RolledBackAt { get; set; }
        public string? Error { get; set; }
    }

    // Auto-Scaling Models
    public class ScaleResult
    {
        public bool Success { get; set; }
        public int CurrentReplicas { get; set; }
        public int PreviousReplicas { get; set; }
        public DateTime ScaledAt { get; set; }
    }

    public class AutoScaleResult
    {
        public bool Scaled { get; set; }
        public int NewReplicas { get; set; }
        public int PreviousReplicas { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime CheckedAt { get; set; }
    }

    public class AutoScalePolicy
    {
        public bool Enabled { get; set; }
        public int MinReplicas { get; set; } = 1;
        public int MaxReplicas { get; set; } = 10;
        public double TargetCPUPercent { get; set; } = 70;
        public double TargetMemoryPercent { get; set; } = 80;
        public int ScaleUpThreshold { get; set; } = 3; // consecutive checks
        public int ScaleDownThreshold { get; set; } = 5; // consecutive checks
        public TimeSpan CooldownPeriod { get; set; } = TimeSpan.FromMinutes(5);
    }

    // Health Monitoring Models
    public class DeploymentHealth
    {
        public Guid DeploymentId { get; set; }
        public HealthStatus Status { get; set; }
        public HealthCheck[] Checks { get; set; } = Array.Empty<HealthCheck>();
        public DateTime LastCheckTime { get; set; }
        public double Uptime { get; set; }
        public int RestartCount { get; set; }
    }

    public class HealthCheck
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Message { get; set; }
        public DateTime CheckedAt { get; set; }
    }

    public class HealthCheckConfig
    {
        public string Type { get; set; } = "http"; // http, tcp, exec
        public string Path { get; set; } = "/health";
        public int Port { get; set; }
        public int IntervalSeconds { get; set; } = 30;
        public int TimeoutSeconds { get; set; } = 10;
        public int HealthyThreshold { get; set; } = 2;
        public int UnhealthyThreshold { get; set; } = 3;
    }

    public class HealthMonitoringReport
    {
        public int TotalDeployments { get; set; }
        public int HealthyDeployments { get; set; }
        public int UnhealthyDeployments { get; set; }
        public int DegradedDeployments { get; set; }
        public List<Guid> UnhealthyDeploymentIds { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    // Load Balancing Models
    public class LoadBalancerConfig
    {
        public LoadBalancingAlgorithm Algorithm { get; set; }
        public string HealthCheckPath { get; set; } = "/health";
        public int HealthCheckInterval { get; set; } = 10;
        public bool StickySession { get; set; }
        public int SessionTimeout { get; set; } = 3600;
        public Dictionary<string, string> Headers { get; set; } = new();
    }

    public class LoadBalancerInfo
    {
        public string Endpoint { get; set; } = string.Empty;
        public int Port { get; set; }
        public int BackendCount { get; set; }
        public LoadBalancingAlgorithm Algorithm { get; set; }
        public bool StickySession { get; set; }
    }

    public class LoadBalancerStats
    {
        public long TotalRequests { get; set; }
        public BackendStats[] BackendStats { get; set; } = Array.Empty<BackendStats>();
        public double AverageResponseTime { get; set; }
        public int ActiveConnections { get; set; }
        public DateTime CollectedAt { get; set; }
    }

    public class BackendStats
    {
        public string InstanceId { get; set; } = string.Empty;
        public long RequestCount { get; set; }
        public string HealthStatus { get; set; } = string.Empty;
        public double ResponseTime { get; set; }
        public int ActiveConnections { get; set; }
    }

    public class LoadBalancerUpdate
    {
        public LoadBalancingAlgorithm? Algorithm { get; set; }
        public bool? StickySession { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
    }

    // Multi-Region Models
    public class MultiRegionDeploymentRequest
    {
        public string ApplicationName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string[] Regions { get; set; } = Array.Empty<string>();
        public int ReplicasPerRegion { get; set; }
        public ResourceRequirements Resources { get; set; } = new();
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
        public LoadBalancerConfig? LoadBalancer { get; set; }
    }

    public class MultiRegionDeploymentResult
    {
        public bool Success { get; set; }
        public List<RegionDeployment> RegionDeployments { get; set; } = new();
        public int TotalReplicas { get; set; }
        public List<string> Errors { get; set; } = new();
        public string GlobalEndpoint { get; set; } = string.Empty;
    }

    public class RegionDeployment
    {
        public string Region { get; set; } = string.Empty;
        public Guid DeploymentId { get; set; }
        public int ReplicaCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
    }

    public class FailoverResult
    {
        public bool Success { get; set; }
        public Guid FromNodeId { get; set; }
        public Guid ToNodeId { get; set; }
        public TimeSpan MigrationTime { get; set; }
        public string? Error { get; set; }
    }

    public class RegionStatus
    {
        public string Region { get; set; } = string.Empty;
        public int NodeCount { get; set; }
        public int HealthyNodes { get; set; }
        public int TotalDeployments { get; set; }
        public int ActiveDeployments { get; set; }
        public NodeCapacity TotalCapacity { get; set; } = new();
        public NodeCapacity AvailableCapacity { get; set; } = new();
    }

    // Configuration Management Models
    public class ConfigurationUpdate
    {
        public Dictionary<string, string> ConfigMap { get; set; } = new();
        public Dictionary<string, string> Secrets { get; set; } = new();
        public bool RestartRequired { get; set; } = true;
    }

    public class ConfigurationUpdateResult
    {
        public bool Success { get; set; }
        public bool RestartRequired { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? Error { get; set; }
    }

    public class ConfigurationHistory
    {
        public Guid Id { get; set; }
        public Guid DeploymentId { get; set; }
        public Dictionary<string, string> Changes { get; set; } = new();
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public string? Comment { get; set; }
    }

    // Performance Optimization Models
    public class OptimizationResult
    {
        public List<OptimizationApplied> OptimizationsApplied { get; set; } = new();
        public double EstimatedImprovement { get; set; }
        public Dictionary<string, double> MetricsBefore { get; set; } = new();
        public Dictionary<string, double> MetricsAfter { get; set; } = new();
    }

    public class OptimizationApplied
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class OptimizationRecommendation
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Impact { get; set; }
        public string Implementation { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    // Container Models
    public class ContainerSpec
    {
        public string Image { get; set; } = string.Empty;
        public string NodeId { get; set; } = string.Empty;
        public Dictionary<string, string> Environment { get; set; } = new();
        public int[] Ports { get; set; } = Array.Empty<int>();
        public ResourceRequirements Resources { get; set; } = new();
        public string[] Command { get; set; } = Array.Empty<string>();
        public Dictionary<string, string> Labels { get; set; } = new();
    }

    public class ContainerInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string NodeId { get; set; } = string.Empty;
        public Dictionary<string, string> NetworkSettings { get; set; } = new();
    }

    public class ContainerHealth
    {
        public HealthStatus Status { get; set; }
        public HealthCheck[] Checks { get; set; } = Array.Empty<HealthCheck>();
        public int RestartCount { get; set; }
        public DateTime LastHealthCheck { get; set; }
    }

    // Kubernetes Models
    public class K8sDeploymentSpec
    {
        public string Name { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public int Replicas { get; set; }
        public Dictionary<string, string> Labels { get; set; } = new();
        public Dictionary<string, string> Environment { get; set; } = new();
        public ResourceRequirements Resources { get; set; } = new();
    }

    public class K8sDeploymentUpdate
    {
        public string? Image { get; set; }
        public int? Replicas { get; set; }
        public Dictionary<string, string>? Environment { get; set; }
    }

    public class K8sDeploymentStatus
    {
        public int ReadyReplicas { get; set; }
        public int AvailableReplicas { get; set; }
        public int UpdatedReplicas { get; set; }
        public Dictionary<string, string> Conditions { get; set; } = new();
    }

    public class K8sServiceSpec
    {
        public string Name { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public Dictionary<string, string> Selector { get; set; } = new();
        public int Port { get; set; }
        public int TargetPort { get; set; }
        public string Type { get; set; } = "ClusterIP";
    }

    public class K8sServiceUpdate
    {
        public int? Port { get; set; }
        public string? Type { get; set; }
    }

    // Registry Models
    public class ImageInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime Created { get; set; }
        public string Digest { get; set; } = string.Empty;
        public Dictionary<string, string> Labels { get; set; } = new();
    }

    public class VulnerabilityScan
    {
        public string Severity { get; set; } = string.Empty;
        public string Package { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string FixedVersion { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    // Metrics Models
    public class NodeMetrics
    {
        public Guid NodeId { get; set; }
        public double CPUUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double DiskUsage { get; set; }
        public double NetworkIn { get; set; }
        public double NetworkOut { get; set; }
        public int RunningContainers { get; set; }
        public DateTime CollectedAt { get; set; }
    }

    public class MetricDataPoint
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new();
    }

    public class AggregatedMetrics
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public double Average { get; set; }
        public double Sum { get; set; }
        public int Count { get; set; }
        public double StandardDeviation { get; set; }
    }

    public class AlertRule
    {
        public string Name { get; set; } = string.Empty;
        public string MetricName { get; set; } = string.Empty;
        public AlertCondition Condition { get; set; }
        public double Threshold { get; set; }
        public TimeSpan Duration { get; set; }
        public AlertSeverity Severity { get; set; }
        public string[] NotificationChannels { get; set; } = Array.Empty<string>();
    }

    // Enums
    public enum NodeStatus
    {
        Ready,
        Busy,
        Maintenance,
        Unhealthy,
        Decommissioned
    }

    public enum DeploymentStatus
    {
        Pending,
        Running,
        Updating,
        Failed,
        Stopped,
        Scaling
    }

    public enum HealthStatus
    {
        Healthy,
        Degraded,
        Unhealthy,
        Unknown
    }

    public enum LoadBalancingAlgorithm
    {
        RoundRobin,
        LeastConnections,
        IPHash,
        Random,
        WeightedRoundRobin
    }

    public enum AggregationType
    {
        Average,
        Sum,
        Min,
        Max,
        Count
    }

    // Deployment Metrics
    public class DeploymentMetrics
    {
        public double CPUUsagePercent { get; set; }
        public double MemoryUsagePercent { get; set; }
        public double NetworkLatency { get; set; }
        public double ErrorRate { get; set; }
        public double CacheMissRate { get; set; }
        public double AverageResponseTime { get; set; }
        public int RequestsPerSecond { get; set; }
    }

    // Options
    public class EdgeDeploymentOptions
    {
        public bool EnableAutoScaling { get; set; }
        public bool EnableHealthChecks { get; set; }
        public int DefaultReplicas { get; set; }
        public int MaxReplicas { get; set; }
        public string RegistryUrl { get; set; } = string.Empty;
        public string ImagePrefix { get; set; } = string.Empty;
        public int HealthCheckIntervalSeconds { get; set; }
        public int DeploymentTimeoutMinutes { get; set; }
        public Dictionary<string, string> DefaultLabels { get; set; } = new();
    }
}