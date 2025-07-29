using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Services
{
    public interface IDockerService
    {
        Task<bool> ValidateNodeAsync(string nodeAddress);
        Task<NodeHealth> GetNodeHealthAsync(string nodeId);
        Task<ContainerInfo> DeployContainerAsync(ContainerSpec spec);
        Task<bool> UpdateContainerAsync(string containerId, ContainerSpec spec);
        Task<bool> ScaleContainerAsync(string containerId, int replicas);
        Task<ContainerHealth> GetContainerHealthAsync(string containerId);
        Task<bool> StopContainerAsync(string containerId);
        Task<bool> RemoveContainerAsync(string containerId);
        Task<bool> DrainNodeAsync(string nodeId);
        Task<bool> MigrateContainerAsync(string containerId, string fromNodeId, string toNodeId);
        Task<bool> ConfigureLoadBalancerAsync(string deploymentId, LoadBalancerConfig config);
        Task<LoadBalancerInfo> GetLoadBalancerInfoAsync(string deploymentId);
        Task<LoadBalancerStats> GetLoadBalancerStatsAsync(string deploymentId);
        Task<bool> UpdateConfigurationAsync(string containerId, Dictionary<string, string> config);
    }

    public interface IKubernetesService
    {
        Task<bool> CreateDeploymentAsync(K8sDeploymentSpec spec);
        Task<bool> UpdateDeploymentAsync(string name, string @namespace, K8sDeploymentUpdate update);
        Task<bool> ScaleDeploymentAsync(string name, string @namespace, int replicas);
        Task<K8sDeploymentStatus> GetDeploymentStatusAsync(string name, string @namespace);
        Task<bool> DeleteDeploymentAsync(string name, string @namespace);
        Task<bool> CreateServiceAsync(K8sServiceSpec spec);
        Task<bool> UpdateServiceAsync(string name, string @namespace, K8sServiceUpdate update);
        Task<bool> CreateConfigMapAsync(string name, string @namespace, Dictionary<string, string> data);
        Task<bool> UpdateConfigMapAsync(string name, string @namespace, Dictionary<string, string> data);
        Task<bool> CreateSecretAsync(string name, string @namespace, Dictionary<string, string> data);
    }

    public interface IRegistryService
    {
        Task<bool> ImageExistsAsync(string imageName, string tag);
        Task<ImageInfo> GetImageInfoAsync(string imageName, string tag);
        Task<List<string>> GetImageTagsAsync(string imageName);
        Task<bool> PushImageAsync(string imageName, string tag, byte[] imageData);
        Task<bool> PullImageAsync(string imageName, string tag);
        Task<bool> DeleteImageAsync(string imageName, string tag);
        Task<List<VulnerabilityScan>> ScanImageAsync(string imageName, string tag);
    }

    public interface IMetricsService
    {
        Task<DeploymentMetrics> GetDeploymentMetricsAsync(Guid deploymentId);
        Task<NodeMetrics> GetNodeMetricsAsync(Guid nodeId);
        Task<List<MetricDataPoint>> GetMetricTimeSeriesAsync(string metricName, DateTime start, DateTime end);
        Task RecordMetricAsync(string metricName, double value, Dictionary<string, string> tags);
        Task<AggregatedMetrics> GetAggregatedMetricsAsync(string metricName, TimeSpan window, AggregationType type);
        Task<List<Alert>> GetActiveAlertsAsync(string? deploymentId = null);
        Task<bool> CreateAlertRuleAsync(AlertRule rule);
    }
}