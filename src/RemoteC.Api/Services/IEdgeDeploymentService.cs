using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Services
{
    public interface IEdgeDeploymentService
    {
        // Edge Node Management
        Task<EdgeNode> RegisterEdgeNodeAsync(EdgeNodeSpec nodeSpec);
        Task<EdgeNodeStatus> GetEdgeNodeStatusAsync(Guid nodeId);
        Task<EdgeNode> UpdateEdgeNodeAsync(Guid nodeId, EdgeNodeUpdate update);
        Task<DecommissionResult> DecommissionNodeAsync(Guid nodeId);
        Task<List<EdgeNode>> GetEdgeNodesAsync(string? location = null, NodeStatus? status = null);
        
        // Deployment Management
        Task<EdgeDeployment> DeployApplicationAsync(EdgeDeploymentRequest request);
        Task<EdgeDeployment> UpdateDeploymentAsync(Guid deploymentId, DeploymentUpdate update);
        Task<RollbackResult> RollbackDeploymentAsync(Guid deploymentId);
        Task<bool> StopDeploymentAsync(Guid deploymentId);
        Task<List<EdgeDeployment>> GetDeploymentsAsync(Guid? nodeId = null, string? applicationName = null);
        
        // Auto-Scaling
        Task<ScaleResult> ScaleDeploymentAsync(Guid deploymentId, int replicas);
        Task<AutoScaleResult> AutoScaleAsync(Guid deploymentId);
        Task<AutoScalePolicy> SetAutoScalePolicyAsync(Guid deploymentId, AutoScalePolicy policy);
        
        // Health Monitoring
        Task<DeploymentHealth> CheckDeploymentHealthAsync(Guid deploymentId);
        Task<HealthMonitoringReport> MonitorAllDeploymentsAsync();
        Task<bool> EnableHealthChecksAsync(Guid deploymentId, HealthCheckConfig config);
        
        // Load Balancing
        Task<LoadBalancerInfo> ConfigureLoadBalancerAsync(Guid deploymentId, LoadBalancerConfig config);
        Task<LoadBalancerStats> GetLoadBalancerStatsAsync(Guid deploymentId);
        Task<bool> UpdateLoadBalancerAsync(Guid deploymentId, LoadBalancerUpdate update);
        
        // Multi-Region Deployment
        Task<MultiRegionDeploymentResult> DeployToMultipleRegionsAsync(MultiRegionDeploymentRequest request);
        Task<FailoverResult> FailoverDeploymentAsync(Guid deploymentId);
        Task<RegionStatus> GetRegionStatusAsync(string region);
        
        // Configuration Management
        Task<ConfigurationUpdateResult> UpdateConfigurationAsync(Guid deploymentId, ConfigurationUpdate update);
        Task<List<ConfigurationHistory>> GetConfigurationHistoryAsync(Guid deploymentId);
        Task<bool> RollbackConfigurationAsync(Guid deploymentId, Guid configVersionId);
        
        // Performance Optimization
        Task<OptimizationResult> OptimizeDeploymentAsync(Guid deploymentId);
        Task<List<OptimizationRecommendation>> GetOptimizationRecommendationsAsync(Guid deploymentId);
        Task<bool> ApplyOptimizationAsync(Guid deploymentId, Guid recommendationId);
    }
}