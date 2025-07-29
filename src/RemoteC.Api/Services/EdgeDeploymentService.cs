using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RemoteC.Data;
using RemoteC.Data.Entities;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Services
{
    public class EdgeDeploymentService : IEdgeDeploymentService
    {
        private readonly RemoteCDbContext _context;
        private readonly ILogger<EdgeDeploymentService> _logger;
        private readonly IDockerService _dockerService;
        private readonly IKubernetesService _kubernetesService;
        private readonly IRegistryService _registryService;
        private readonly IMetricsService _metricsService;
        private readonly EdgeDeploymentOptions _options;

        public EdgeDeploymentService(
            RemoteCDbContext context,
            ILogger<EdgeDeploymentService> logger,
            IDockerService dockerService,
            IKubernetesService kubernetesService,
            IRegistryService registryService,
            IMetricsService metricsService,
            IOptions<EdgeDeploymentOptions> options)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dockerService = dockerService ?? throw new ArgumentNullException(nameof(dockerService));
            _kubernetesService = kubernetesService ?? throw new ArgumentNullException(nameof(kubernetesService));
            _registryService = registryService ?? throw new ArgumentNullException(nameof(registryService));
            _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        #region Edge Node Management

        public async Task<EdgeNode> RegisterEdgeNodeAsync(EdgeNodeSpec nodeSpec)
        {
            // Check for duplicate node name
            var existingNode = await _context.EdgeNodes
                .FirstOrDefaultAsync(n => n.Name == nodeSpec.Name);
            
            if (existingNode != null)
            {
                throw new InvalidOperationException($"Node with name '{nodeSpec.Name}' already exists");
            }

            // Validate node connectivity
            var nodeAddress = $"{nodeSpec.IpAddress}:{nodeSpec.Port}";
            var isValid = await _dockerService.ValidateNodeAsync(nodeAddress);
            
            if (!isValid)
            {
                throw new InvalidOperationException($"Cannot connect to node at {nodeAddress}");
            }

            // Create new node entity
            var nodeEntity = new EdgeNodeEntity
            {
                Id = Guid.NewGuid(),
                Name = nodeSpec.Name,
                Location = nodeSpec.Location,
                Capacity = nodeSpec.Capacity,
                AvailableCapacity = nodeSpec.Capacity, // Initially all capacity is available
                LabelsJson = System.Text.Json.JsonSerializer.Serialize(nodeSpec.Labels ?? new Dictionary<string, string>()),
                Status = NodeStatus.Ready,
                RegisteredAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow,
                IpAddress = nodeSpec.IpAddress,
                Port = nodeSpec.Port,
                Version = "1.0.0" // TODO: Get from node
            };

            _context.EdgeNodes.Add(nodeEntity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Registered new edge node {NodeName} at {Location}", nodeEntity.Name, nodeEntity.Location);

            return nodeEntity.ToModel();
        }

        public async Task<EdgeNodeStatus> GetEdgeNodeStatusAsync(Guid nodeId)
        {
            var nodeEntity = await _context.EdgeNodes.FindAsync(nodeId);
            if (nodeEntity == null)
            {
                throw new ArgumentException($"Node {nodeId} not found");
            }

            // Get health from Docker service
            var health = await _dockerService.GetNodeHealthAsync(nodeId.ToString());

            // Count active deployments
            var activeDeployments = await _context.EdgeDeployments
                .CountAsync(d => d.NodeId == nodeId && d.Status == DeploymentStatus.Running);

            // Calculate used capacity
            var deployments = await _context.EdgeDeployments
                .Where(d => d.NodeId == nodeId && d.Status == DeploymentStatus.Running)
                .ToListAsync();

            var usedCapacity = new NodeCapacity
            {
                CPU = 0,
                MemoryGB = 0,
                StorageGB = 0,
                NetworkMbps = 0
            };

            foreach (var deployment in deployments)
            {
                var resources = System.Text.Json.JsonSerializer.Deserialize<ResourceRequirements>(deployment.ResourcesJson) ?? new ResourceRequirements();
                usedCapacity.CPU += (int)(resources.CPU * deployment.Replicas);
                usedCapacity.MemoryGB += (int)(resources.MemoryGB * deployment.Replicas);
                usedCapacity.StorageGB += resources.StorageGB * deployment.Replicas;
                usedCapacity.NetworkMbps += resources.NetworkMbps ?? 0;
            }

            return new EdgeNodeStatus
            {
                NodeId = nodeId,
                Health = health,
                IsOnline = health.Status == HealthStatus.Healthy,
                ActiveDeployments = activeDeployments,
                UsedCapacity = usedCapacity,
                LastHealthCheck = DateTime.UtcNow
            };
        }

        public async Task<EdgeNode> UpdateEdgeNodeAsync(Guid nodeId, EdgeNodeUpdate update)
        {
            var nodeEntity = await _context.EdgeNodes.FindAsync(nodeId);
            if (nodeEntity == null)
            {
                throw new ArgumentException($"Node {nodeId} not found");
            }

            if (update.Labels != null)
            {
                nodeEntity.LabelsJson = System.Text.Json.JsonSerializer.Serialize(update.Labels);
            }

            if (update.Capacity != null)
            {
                var capacityDiff = new NodeCapacity
                {
                    CPU = update.Capacity.CPU - nodeEntity.Capacity.CPU,
                    MemoryGB = update.Capacity.MemoryGB - nodeEntity.Capacity.MemoryGB,
                    StorageGB = update.Capacity.StorageGB - nodeEntity.Capacity.StorageGB,
                    NetworkMbps = update.Capacity.NetworkMbps - nodeEntity.Capacity.NetworkMbps
                };

                nodeEntity.Capacity = update.Capacity;
                nodeEntity.AvailableCapacity = new NodeCapacity
                {
                    CPU = nodeEntity.AvailableCapacity.CPU + capacityDiff.CPU,
                    MemoryGB = nodeEntity.AvailableCapacity.MemoryGB + capacityDiff.MemoryGB,
                    StorageGB = nodeEntity.AvailableCapacity.StorageGB + capacityDiff.StorageGB,
                    NetworkMbps = nodeEntity.AvailableCapacity.NetworkMbps + capacityDiff.NetworkMbps
                };
            }

            if (update.Status.HasValue)
            {
                nodeEntity.Status = update.Status.Value;
            }

            nodeEntity.LastSeenAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return nodeEntity.ToModel();
        }

        public async Task<DecommissionResult> DecommissionNodeAsync(Guid nodeId)
        {
            var nodeEntity = await _context.EdgeNodes.FindAsync(nodeId);
            if (nodeEntity == null)
            {
                throw new ArgumentException($"Node {nodeId} not found");
            }

            var result = new DecommissionResult
            {
                MigratedDeployments = new List<Guid>(),
                Errors = new List<string>()
            };

            // Get all deployments on this node
            var deployments = await _context.EdgeDeployments
                .Where(d => d.NodeId == nodeId && d.Status == DeploymentStatus.Running)
                .ToListAsync();

            // Drain the node
            var drainSuccess = await _dockerService.DrainNodeAsync(nodeId.ToString());
            if (!drainSuccess)
            {
                result.Errors.Add("Failed to drain node");
            }

            // Mark node as decommissioned
            nodeEntity.Status = NodeStatus.Decommissioned;
            await _context.SaveChangesAsync();

            result.Success = result.Errors.Count == 0;
            result.CompletedAt = DateTime.UtcNow;

            return result;
        }

        public async Task<List<EdgeNode>> GetEdgeNodesAsync(string? location = null, NodeStatus? status = null)
        {
            var query = _context.EdgeNodes.AsQueryable();

            if (!string.IsNullOrEmpty(location))
            {
                query = query.Where(n => n.Location == location);
            }

            if (status.HasValue)
            {
                query = query.Where(n => n.Status == status.Value);
            }

            var entities = await query.ToListAsync();
            return entities.Select(e => e.ToModel()).ToList();
        }

        #endregion

        #region Deployment Management

        public async Task<EdgeDeployment> DeployApplicationAsync(EdgeDeploymentRequest request)
        {
            // Find target node
            EdgeNodeEntity? targetNodeEntity = null;
            
            if (request.NodeId.HasValue)
            {
                targetNodeEntity = await _context.EdgeNodes.FindAsync(request.NodeId.Value);
                if (targetNodeEntity == null)
                {
                    throw new ArgumentException($"Node {request.NodeId.Value} not found");
                }
            }
            else if (!string.IsNullOrEmpty(request.NodeLocation))
            {
                targetNodeEntity = await _context.EdgeNodes
                    .FirstOrDefaultAsync(n => n.Location == request.NodeLocation && n.Status == NodeStatus.Ready);
                
                if (targetNodeEntity == null)
                {
                    throw new InvalidOperationException($"No available nodes found in location {request.NodeLocation}");
                }
            }
            else
            {
                // Find best available node
                targetNodeEntity = await FindBestNodeForDeployment(request.Resources);
                if (targetNodeEntity == null)
                {
                    throw new InvalidOperationException("No nodes have sufficient resources for deployment");
                }
            }

            // Validate resources
            if (!HasSufficientResources(targetNodeEntity, request.Resources))
            {
                throw new InvalidOperationException($"Node {targetNodeEntity.Name} has insufficient resources");
            }

            // Verify image exists
            var imageName = $"{_options.ImagePrefix}/{request.ApplicationName}";
            var imageExists = await _registryService.ImageExistsAsync(imageName, request.Version);
            
            if (!imageExists)
            {
                throw new ArgumentException($"Image {imageName}:{request.Version} not found in registry");
            }

            // Create container spec
            var containerSpec = new ContainerSpec
            {
                Image = $"{imageName}:{request.Version}",
                NodeId = targetNodeEntity.Id.ToString(),
                Environment = request.EnvironmentVariables,
                Ports = request.Ports,
                Resources = request.Resources,
                Labels = new Dictionary<string, string>
                {
                    ["app"] = request.ApplicationName,
                    ["version"] = request.Version
                }
            };

            // Deploy container
            var containerInfo = await _dockerService.DeployContainerAsync(containerSpec);

            // Create deployment entity
            var deploymentEntity = new EdgeDeploymentEntity
            {
                Id = Guid.NewGuid(),
                ApplicationName = request.ApplicationName,
                Version = request.Version,
                NodeId = targetNodeEntity.Id,
                Status = DeploymentStatus.Running,
                Replicas = 1,
                ResourcesJson = System.Text.Json.JsonSerializer.Serialize(request.Resources),
                EnvironmentVariablesJson = System.Text.Json.JsonSerializer.Serialize(request.EnvironmentVariables),
                PortsJson = System.Text.Json.JsonSerializer.Serialize(request.Ports),
                CreatedAt = DateTime.UtcNow,
                ContainerId = containerInfo.Id
            };

            _context.EdgeDeployments.Add(deploymentEntity);

            // Update node capacity
            targetNodeEntity.AvailableCapacity = new NodeCapacity
            {
                CPU = targetNodeEntity.AvailableCapacity.CPU - (int)(request.Resources.CPU),
                MemoryGB = targetNodeEntity.AvailableCapacity.MemoryGB - (int)(request.Resources.MemoryGB),
                StorageGB = targetNodeEntity.AvailableCapacity.StorageGB - request.Resources.StorageGB,
                NetworkMbps = targetNodeEntity.AvailableCapacity.NetworkMbps - (request.Resources.NetworkMbps ?? 0)
            };

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Deployed {ApplicationName}:{Version} on node {NodeName}",
                request.ApplicationName, request.Version, targetNodeEntity.Name);

            return deploymentEntity.ToModel();
        }

        public async Task<EdgeDeployment> UpdateDeploymentAsync(Guid deploymentId, DeploymentUpdate update)
        {
            var deploymentEntity = await _context.EdgeDeployments.FindAsync(deploymentId);
            if (deploymentEntity == null)
            {
                throw new ArgumentException($"Deployment {deploymentId} not found");
            }

            var updateNeeded = false;
            var deployment = deploymentEntity.ToModel();
            var containerSpec = new ContainerSpec
            {
                Image = $"{_options.ImagePrefix}/{deployment.ApplicationName}:{deployment.Version}",
                NodeId = deployment.NodeId.ToString(),
                Environment = deployment.EnvironmentVariables,
                Ports = deployment.Ports,
                Resources = deployment.Resources
            };

            if (!string.IsNullOrEmpty(update.Version))
            {
                // Verify new version exists
                var imageName = $"{_options.ImagePrefix}/{deploymentEntity.ApplicationName}";
                var imageExists = await _registryService.ImageExistsAsync(imageName, update.Version);
                
                if (!imageExists)
                {
                    throw new ArgumentException($"Image {imageName}:{update.Version} not found in registry");
                }

                deploymentEntity.PreviousVersion = deploymentEntity.Version;
                deploymentEntity.Version = update.Version;
                containerSpec.Image = $"{imageName}:{update.Version}";
                updateNeeded = true;
            }

            if (update.EnvironmentVariables != null)
            {
                deploymentEntity.EnvironmentVariablesJson = System.Text.Json.JsonSerializer.Serialize(update.EnvironmentVariables);
                containerSpec.Environment = update.EnvironmentVariables;
                updateNeeded = true;
            }

            if (update.Resources != null)
            {
                deploymentEntity.ResourcesJson = System.Text.Json.JsonSerializer.Serialize(update.Resources);
                containerSpec.Resources = update.Resources;
                updateNeeded = true;
            }

            if (update.Replicas.HasValue)
            {
                deploymentEntity.Replicas = update.Replicas.Value;
                // Scaling is handled separately
            }

            if (updateNeeded)
            {
                deploymentEntity.Status = DeploymentStatus.Updating;
                await _context.SaveChangesAsync();

                var success = await _dockerService.UpdateContainerAsync(deploymentEntity.ContainerId, containerSpec);
                
                deploymentEntity.Status = success ? DeploymentStatus.Running : DeploymentStatus.Failed;
                deploymentEntity.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return deploymentEntity.ToModel();
        }

        public async Task<RollbackResult> RollbackDeploymentAsync(Guid deploymentId)
        {
            var deployment = await _context.EdgeDeployments.FindAsync(deploymentId);
            if (deployment == null)
            {
                throw new ArgumentException($"Deployment {deploymentId} not found");
            }

            if (string.IsNullOrEmpty(deployment.PreviousVersion))
            {
                return new RollbackResult
                {
                    Success = false,
                    Error = "No previous version available for rollback",
                    CurrentVersion = deployment.Version,
                    RolledBackFrom = deployment.Version,
                    RolledBackAt = DateTime.UtcNow
                };
            }

            var containerSpec = new ContainerSpec
            {
                Image = $"{_options.ImagePrefix}/{deployment.ApplicationName}:{deployment.PreviousVersion}",
                NodeId = deployment.NodeId.ToString(),
                Environment = deployment.EnvironmentVariables,
                Ports = deployment.Ports,
                Resources = deployment.Resources
            };

            var success = await _dockerService.UpdateContainerAsync(deployment.ContainerId, containerSpec);

            if (success)
            {
                var currentVersion = deployment.Version;
                deployment.Version = deployment.PreviousVersion;
                deployment.PreviousVersion = currentVersion;
                deployment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return new RollbackResult
            {
                Success = success,
                CurrentVersion = deployment.Version,
                RolledBackFrom = deployment.PreviousVersion ?? deployment.Version,
                RolledBackAt = DateTime.UtcNow,
                Error = success ? null : "Failed to update container"
            };
        }

        public async Task<bool> StopDeploymentAsync(Guid deploymentId)
        {
            var deployment = await _context.EdgeDeployments.FindAsync(deploymentId);
            if (deployment == null)
            {
                throw new ArgumentException($"Deployment {deploymentId} not found");
            }

            var success = await _dockerService.StopContainerAsync(deployment.ContainerId);
            
            if (success)
            {
                deployment.Status = DeploymentStatus.Stopped;
                deployment.UpdatedAt = DateTime.UtcNow;

                // Free up node resources
                var node = await _context.EdgeNodes.FindAsync(deployment.NodeId);
                if (node != null)
                {
                    node.AvailableCapacity = new NodeCapacity
                    {
                        CPU = node.AvailableCapacity.CPU + (int)(deployment.Resources.CPU * deployment.Replicas),
                        MemoryGB = node.AvailableCapacity.MemoryGB + (int)(deployment.Resources.MemoryGB * deployment.Replicas),
                        StorageGB = node.AvailableCapacity.StorageGB + deployment.Resources.StorageGB * deployment.Replicas,
                        NetworkMbps = node.AvailableCapacity.NetworkMbps + (deployment.Resources.NetworkMbps ?? 0)
                    };
                }

                await _context.SaveChangesAsync();
            }

            return success;
        }

        public async Task<List<EdgeDeployment>> GetDeploymentsAsync(Guid? nodeId = null, string? applicationName = null)
        {
            var query = _context.EdgeDeployments.AsQueryable();

            if (nodeId.HasValue)
            {
                query = query.Where(d => d.NodeId == nodeId.Value);
            }

            if (!string.IsNullOrEmpty(applicationName))
            {
                query = query.Where(d => d.ApplicationName == applicationName);
            }

            var entities = await query.ToListAsync();
            return entities.Select(e => e.ToModel()).ToList();
        }

        #endregion

        #region Auto-Scaling

        public async Task<ScaleResult> ScaleDeploymentAsync(Guid deploymentId, int replicas)
        {
            var deployment = await _context.EdgeDeployments.FindAsync(deploymentId);
            if (deployment == null)
            {
                throw new ArgumentException($"Deployment {deploymentId} not found");
            }

            var previousReplicas = deployment.Replicas;
            var success = await _dockerService.ScaleContainerAsync(deployment.ContainerId, replicas);

            if (success)
            {
                deployment.Replicas = replicas;
                deployment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return new ScaleResult
            {
                Success = success,
                CurrentReplicas = deployment.Replicas,
                PreviousReplicas = previousReplicas,
                ScaledAt = DateTime.UtcNow
            };
        }

        public async Task<AutoScaleResult> AutoScaleAsync(Guid deploymentId)
        {
            var deployment = await _context.EdgeDeployments
                .Include(d => d.AutoScalePolicy)
                .FirstOrDefaultAsync(d => d.Id == deploymentId);

            if (deployment == null)
            {
                throw new ArgumentException($"Deployment {deploymentId} not found");
            }

            var policy = deployment.AutoScalePolicy ?? new AutoScalePolicy
            {
                Enabled = _options.EnableAutoScaling,
                MinReplicas = 1,
                MaxReplicas = _options.MaxReplicas,
                TargetCPUPercent = 70,
                TargetMemoryPercent = 80
            };

            if (!policy.Enabled)
            {
                return new AutoScaleResult
                {
                    Scaled = false,
                    NewReplicas = deployment.Replicas,
                    PreviousReplicas = deployment.Replicas,
                    Reason = "Auto-scaling is disabled",
                    CheckedAt = DateTime.UtcNow
                };
            }

            // Get current metrics
            var metrics = await _metricsService.GetDeploymentMetricsAsync(deploymentId);
            
            var currentReplicas = deployment.Replicas;
            var targetReplicas = currentReplicas;
            var reason = "";

            // Check if scaling is needed
            if (metrics.CPUUsagePercent > policy.TargetCPUPercent)
            {
                // Scale up
                var scaleFactor = metrics.CPUUsagePercent / policy.TargetCPUPercent;
                targetReplicas = (int)Math.Ceiling(currentReplicas * scaleFactor);
                reason = $"High CPU usage ({metrics.CPUUsagePercent:F1}%)";
            }
            else if (metrics.MemoryUsagePercent > policy.TargetMemoryPercent)
            {
                // Scale up
                var scaleFactor = metrics.MemoryUsagePercent / policy.TargetMemoryPercent;
                targetReplicas = (int)Math.Ceiling(currentReplicas * scaleFactor);
                reason = $"High memory usage ({metrics.MemoryUsagePercent:F1}%)";
            }
            else if (metrics.CPUUsagePercent < policy.TargetCPUPercent * 0.5 && 
                     metrics.MemoryUsagePercent < policy.TargetMemoryPercent * 0.5 &&
                     currentReplicas > policy.MinReplicas)
            {
                // Scale down
                targetReplicas = Math.Max(policy.MinReplicas, currentReplicas - 1);
                reason = "Low resource usage";
            }

            // Apply limits
            targetReplicas = Math.Max(policy.MinReplicas, Math.Min(policy.MaxReplicas, targetReplicas));

            if (targetReplicas == currentReplicas)
            {
                return new AutoScaleResult
                {
                    Scaled = false,
                    NewReplicas = currentReplicas,
                    PreviousReplicas = currentReplicas,
                    Reason = targetReplicas == policy.MaxReplicas ? "Already at maximum replicas" : "No scaling needed",
                    CheckedAt = DateTime.UtcNow
                };
            }

            // Perform scaling
            var scaleResult = await ScaleDeploymentAsync(deploymentId, targetReplicas);

            return new AutoScaleResult
            {
                Scaled = scaleResult.Success,
                NewReplicas = scaleResult.CurrentReplicas,
                PreviousReplicas = scaleResult.PreviousReplicas,
                Reason = reason,
                CheckedAt = DateTime.UtcNow
            };
        }

        public async Task<AutoScalePolicy> SetAutoScalePolicyAsync(Guid deploymentId, AutoScalePolicy policy)
        {
            var deployment = await _context.EdgeDeployments.FindAsync(deploymentId);
            if (deployment == null)
            {
                throw new ArgumentException($"Deployment {deploymentId} not found");
            }

            deployment.AutoScalePolicyJson = System.Text.Json.JsonSerializer.Serialize(policy);
            await _context.SaveChangesAsync();

            return policy;
        }

        #endregion

        #region Health Monitoring

        public async Task<DeploymentHealth> CheckDeploymentHealthAsync(Guid deploymentId)
        {
            var deployment = await _context.EdgeDeployments.FindAsync(deploymentId);
            if (deployment == null)
            {
                throw new ArgumentException($"Deployment {deploymentId} not found");
            }

            var containerHealth = await _dockerService.GetContainerHealthAsync(deployment.ContainerId);

            return new DeploymentHealth
            {
                DeploymentId = deploymentId,
                Status = containerHealth.Status,
                Checks = containerHealth.Checks,
                LastCheckTime = DateTime.UtcNow,
                Uptime = (DateTime.UtcNow - deployment.CreatedAt).TotalHours,
                RestartCount = containerHealth.RestartCount
            };
        }

        public async Task<HealthMonitoringReport> MonitorAllDeploymentsAsync()
        {
            var deployments = await _context.EdgeDeployments
                .Where(d => d.Status == DeploymentStatus.Running)
                .ToListAsync();

            var report = new HealthMonitoringReport
            {
                TotalDeployments = deployments.Count,
                HealthyDeployments = 0,
                UnhealthyDeployments = 0,
                DegradedDeployments = 0,
                UnhealthyDeploymentIds = new List<Guid>(),
                GeneratedAt = DateTime.UtcNow
            };

            foreach (var deployment in deployments)
            {
                try
                {
                    var health = await CheckDeploymentHealthAsync(deployment.Id);
                    
                    switch (health.Status)
                    {
                        case HealthStatus.Healthy:
                            report.HealthyDeployments++;
                            break;
                        case HealthStatus.Degraded:
                            report.DegradedDeployments++;
                            break;
                        case HealthStatus.Unhealthy:
                            report.UnhealthyDeployments++;
                            report.UnhealthyDeploymentIds.Add(deployment.Id);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking health for deployment {DeploymentId}", deployment.Id);
                    report.UnhealthyDeployments++;
                    report.UnhealthyDeploymentIds.Add(deployment.Id);
                }
            }

            return report;
        }

        public async Task<bool> EnableHealthChecksAsync(Guid deploymentId, HealthCheckConfig config)
        {
            var deployment = await _context.EdgeDeployments.FindAsync(deploymentId);
            if (deployment == null)
            {
                throw new ArgumentException($"Deployment {deploymentId} not found");
            }

            deployment.HealthCheckConfigJson = System.Text.Json.JsonSerializer.Serialize(config);
            await _context.SaveChangesAsync();

            // TODO: Configure health checks in Docker/K8s
            return true;
        }

        #endregion

        #region Load Balancing

        public async Task<LoadBalancerInfo> ConfigureLoadBalancerAsync(Guid deploymentId, LoadBalancerConfig config)
        {
            var deployment = await _context.EdgeDeployments.FindAsync(deploymentId);
            if (deployment == null)
            {
                throw new ArgumentException($"Deployment {deploymentId} not found");
            }

            var lbInfo = await _dockerService.GetLoadBalancerInfoAsync(deploymentId.ToString());
            
            if (lbInfo == null)
            {
                // Create new load balancer
                await _dockerService.ConfigureLoadBalancerAsync(deploymentId.ToString(), config);
                lbInfo = new LoadBalancerInfo
                {
                    Endpoint = "lb.remotec.io", // TODO: Get from config
                    Port = 443,
                    BackendCount = deployment.Replicas,
                    Algorithm = config.Algorithm,
                    StickySession = config.StickySession
                };
            }

            deployment.LoadBalancerJson = System.Text.Json.JsonSerializer.Serialize(lbInfo);
            await _context.SaveChangesAsync();

            return lbInfo;
        }

        public async Task<LoadBalancerStats> GetLoadBalancerStatsAsync(Guid deploymentId)
        {
            var deployment = await _context.EdgeDeployments.FindAsync(deploymentId);
            if (deployment == null)
            {
                throw new ArgumentException($"Deployment {deploymentId} not found");
            }

            return await _dockerService.GetLoadBalancerStatsAsync(deploymentId.ToString());
        }

        public async Task<bool> UpdateLoadBalancerAsync(Guid deploymentId, LoadBalancerUpdate update)
        {
            var deployment = await _context.EdgeDeployments.FindAsync(deploymentId);
            if (deployment == null)
            {
                throw new ArgumentException($"Deployment {deploymentId} not found");
            }

            if (deployment.LoadBalancer == null)
            {
                throw new InvalidOperationException($"No load balancer configured for deployment {deploymentId}");
            }

            // Update load balancer config
            var config = new LoadBalancerConfig
            {
                Algorithm = update.Algorithm ?? deployment.LoadBalancer.Algorithm,
                StickySession = update.StickySession ?? deployment.LoadBalancer.StickySession,
                Headers = update.Headers ?? new Dictionary<string, string>()
            };

            return await _dockerService.ConfigureLoadBalancerAsync(deploymentId.ToString(), config);
        }

        #endregion

        #region Multi-Region Deployment

        public async Task<MultiRegionDeploymentResult> DeployToMultipleRegionsAsync(MultiRegionDeploymentRequest request)
        {
            var result = new MultiRegionDeploymentResult
            {
                Success = true,
                RegionDeployments = new List<RegionDeployment>(),
                TotalReplicas = 0,
                Errors = new List<string>()
            };

            foreach (var region in request.Regions)
            {
                try
                {
                    // Find nodes in the region
                    var nodes = await GetEdgeNodesAsync(region, NodeStatus.Ready);
                    
                    if (nodes.Count == 0)
                    {
                        result.Errors.Add($"No available nodes in region {region}");
                        continue;
                    }

                    // Deploy to the region
                    var deploymentRequest = new EdgeDeploymentRequest
                    {
                        ApplicationName = request.ApplicationName,
                        Version = request.Version,
                        NodeLocation = region,
                        Resources = request.Resources,
                        EnvironmentVariables = request.EnvironmentVariables,
                        Ports = new int[] { } // Will be assigned by load balancer
                    };

                    var deployment = await DeployApplicationAsync(deploymentRequest);
                    
                    // Scale to requested replicas
                    if (request.ReplicasPerRegion > 1)
                    {
                        await ScaleDeploymentAsync(deployment.Id, request.ReplicasPerRegion);
                    }

                    var regionDeployment = new RegionDeployment
                    {
                        Region = region,
                        DeploymentId = deployment.Id,
                        ReplicaCount = request.ReplicasPerRegion,
                        Status = "Active",
                        Endpoint = $"{region}.{request.ApplicationName}.remotec.io"
                    };

                    result.RegionDeployments.Add(regionDeployment);
                    result.TotalReplicas += request.ReplicasPerRegion;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to deploy to region {Region}", region);
                    result.Errors.Add($"Failed to deploy to {region}: {ex.Message}");
                    result.Success = false;
                }
            }

            // Configure global load balancer if requested
            if (request.LoadBalancer != null && result.RegionDeployments.Count > 0)
            {
                // TODO: Configure global load balancer across regions
                result.GlobalEndpoint = $"global.{request.ApplicationName}.remotec.io";
            }

            return result;
        }

        public async Task<FailoverResult> FailoverDeploymentAsync(Guid deploymentId)
        {
            var deployment = await _context.EdgeDeployments.FindAsync(deploymentId);
            if (deployment == null)
            {
                throw new ArgumentException($"Deployment {deploymentId} not found");
            }

            var fromNode = await _context.EdgeNodes.FindAsync(deployment.NodeId);
            if (fromNode == null)
            {
                throw new InvalidOperationException($"Source node {deployment.NodeId} not found");
            }

            // Check if node is unhealthy
            var nodeHealth = await _dockerService.GetNodeHealthAsync(fromNode.Id.ToString());
            if (nodeHealth.Status == HealthStatus.Healthy)
            {
                return new FailoverResult
                {
                    Success = false,
                    FromNodeId = fromNode.Id,
                    ToNodeId = fromNode.Id,
                    Error = "Source node is healthy, failover not needed",
                    MigrationTime = TimeSpan.Zero
                };
            }

            // Find healthy node in same location
            var healthyNode = await _context.EdgeNodes
                .Where(n => n.Location == fromNode.Location && 
                           n.Id != fromNode.Id && 
                           n.Status == NodeStatus.Ready)
                .FirstOrDefaultAsync();

            if (healthyNode == null)
            {
                return new FailoverResult
                {
                    Success = false,
                    FromNodeId = fromNode.Id,
                    ToNodeId = Guid.Empty,
                    Error = $"No healthy nodes available in location {fromNode.Location}",
                    MigrationTime = TimeSpan.Zero
                };
            }

            // Check node health
            var targetNodeHealth = await _dockerService.GetNodeHealthAsync(healthyNode.Id.ToString());
            if (targetNodeHealth.Status != HealthStatus.Healthy)
            {
                return new FailoverResult
                {
                    Success = false,
                    FromNodeId = fromNode.Id,
                    ToNodeId = healthyNode.Id,
                    Error = "Target node is not healthy",
                    MigrationTime = TimeSpan.Zero
                };
            }

            // Perform migration
            var startTime = DateTime.UtcNow;
            var success = await _dockerService.MigrateContainerAsync(
                deployment.ContainerId, 
                fromNode.Id.ToString(), 
                healthyNode.Id.ToString());

            if (success)
            {
                deployment.NodeId = healthyNode.Id;
                deployment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return new FailoverResult
            {
                Success = success,
                FromNodeId = fromNode.Id,
                ToNodeId = healthyNode.Id,
                MigrationTime = DateTime.UtcNow - startTime,
                Error = success ? null : "Migration failed"
            };
        }

        public async Task<RegionStatus> GetRegionStatusAsync(string region)
        {
            var nodes = await GetEdgeNodesAsync(region);
            var healthyNodes = nodes.Count(n => n.Status == NodeStatus.Ready);

            var deployments = await _context.EdgeDeployments
                .Where(d => nodes.Select(n => n.Id).Contains(d.NodeId))
                .ToListAsync();

            var totalCapacity = new NodeCapacity
            {
                CPU = nodes.Sum(n => n.Capacity.CPU),
                MemoryGB = nodes.Sum(n => n.Capacity.MemoryGB),
                StorageGB = nodes.Sum(n => n.Capacity.StorageGB),
                NetworkMbps = nodes.Sum(n => n.Capacity.NetworkMbps)
            };

            var availableCapacity = new NodeCapacity
            {
                CPU = nodes.Sum(n => n.AvailableCapacity.CPU),
                MemoryGB = nodes.Sum(n => n.AvailableCapacity.MemoryGB),
                StorageGB = nodes.Sum(n => n.AvailableCapacity.StorageGB),
                NetworkMbps = nodes.Sum(n => n.AvailableCapacity.NetworkMbps)
            };

            return new RegionStatus
            {
                Region = region,
                NodeCount = nodes.Count,
                HealthyNodes = healthyNodes,
                TotalDeployments = deployments.Count,
                ActiveDeployments = deployments.Count(d => d.Status == DeploymentStatus.Running),
                TotalCapacity = totalCapacity,
                AvailableCapacity = availableCapacity
            };
        }

        #endregion

        #region Configuration Management

        public async Task<ConfigurationUpdateResult> UpdateConfigurationAsync(Guid deploymentId, ConfigurationUpdate update)
        {
            var deployment = await _context.EdgeDeployments.FindAsync(deploymentId);
            if (deployment == null)
            {
                throw new ArgumentException($"Deployment {deploymentId} not found");
            }

            try
            {
                // Merge config maps
                var allConfig = new Dictionary<string, string>(deployment.EnvironmentVariables);
                foreach (var kvp in update.ConfigMap)
                {
                    allConfig[kvp.Key] = kvp.Value;
                }

                // Add secrets (prefixed for security)
                foreach (var kvp in update.Secrets)
                {
                    allConfig[$"SECRET_{kvp.Key}"] = kvp.Value;
                }

                var success = await _dockerService.UpdateConfigurationAsync(deployment.ContainerId, allConfig);

                if (success)
                {
                    // Track configuration history
                    var history = new ConfigurationHistoryEntity
                    {
                        Id = Guid.NewGuid(),
                        DeploymentId = deploymentId,
                        ChangesJson = System.Text.Json.JsonSerializer.Serialize(update.ConfigMap),
                        UpdatedBy = "system", // TODO: Get from context
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.ConfigurationHistories.Add(history);
                    await _context.SaveChangesAsync();
                }

                return new ConfigurationUpdateResult
                {
                    Success = success,
                    RestartRequired = update.RestartRequired,
                    UpdatedAt = DateTime.UtcNow,
                    Error = success ? null : "Failed to update container configuration"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating configuration for deployment {DeploymentId}", deploymentId);
                return new ConfigurationUpdateResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<List<ConfigurationHistory>> GetConfigurationHistoryAsync(Guid deploymentId)
        {
            var entities = await _context.ConfigurationHistories
                .Where(h => h.DeploymentId == deploymentId)
                .OrderByDescending(h => h.UpdatedAt)
                .ToListAsync();
                
            return entities.Select(e => e.ToModel()).ToList();
        }

        public async Task<bool> RollbackConfigurationAsync(Guid deploymentId, Guid configVersionId)
        {
            var history = await _context.ConfigurationHistories.FindAsync(configVersionId);
            if (history == null || history.DeploymentId != deploymentId)
            {
                throw new ArgumentException($"Configuration version {configVersionId} not found for deployment {deploymentId}");
            }

            var update = new ConfigurationUpdate
            {
                ConfigMap = history.Changes,
                RestartRequired = true
            };

            var result = await UpdateConfigurationAsync(deploymentId, update);
            return result.Success;
        }

        #endregion

        #region Performance Optimization

        public async Task<OptimizationResult> OptimizeDeploymentAsync(Guid deploymentId)
        {
            var deployment = await _context.EdgeDeployments.FindAsync(deploymentId);
            if (deployment == null)
            {
                throw new ArgumentException($"Deployment {deploymentId} not found");
            }

            var result = new OptimizationResult
            {
                OptimizationsApplied = new List<OptimizationApplied>(),
                MetricsBefore = new Dictionary<string, double>(),
                MetricsAfter = new Dictionary<string, double>()
            };

            // Get current metrics
            var metrics = await _metricsService.GetDeploymentMetricsAsync(deploymentId);
            
            result.MetricsBefore["cpu_usage"] = metrics.CPUUsagePercent;
            result.MetricsBefore["memory_usage"] = metrics.MemoryUsagePercent;
            result.MetricsBefore["response_time"] = metrics.AverageResponseTime;
            result.MetricsBefore["cache_miss_rate"] = metrics.CacheMissRate;

            // Apply optimizations based on metrics
            if (metrics.CacheMissRate > 0.5)
            {
                // Increase cache size
                var cacheConfig = new Dictionary<string, string>
                {
                    ["CACHE_SIZE_MB"] = "512",
                    ["CACHE_TTL_SECONDS"] = "3600"
                };

                await _dockerService.UpdateConfigurationAsync(deployment.ContainerId, cacheConfig);

                result.OptimizationsApplied.Add(new OptimizationApplied
                {
                    Type = "cache",
                    Description = "Increased cache size and TTL",
                    Parameters = new Dictionary<string, object> { ["size_mb"] = 512, ["ttl_seconds"] = 3600 }
                });
            }

            if (metrics.CPUUsagePercent > 80)
            {
                // Enable CPU optimization
                var cpuConfig = new Dictionary<string, string>
                {
                    ["CPU_OPTIMIZATION"] = "enabled",
                    ["WORKER_THREADS"] = Math.Max(2, Environment.ProcessorCount).ToString()
                };

                await _dockerService.UpdateConfigurationAsync(deployment.ContainerId, cpuConfig);

                result.OptimizationsApplied.Add(new OptimizationApplied
                {
                    Type = "cpu",
                    Description = "Enabled CPU optimization and adjusted worker threads",
                    Parameters = new Dictionary<string, object> { ["worker_threads"] = Environment.ProcessorCount }
                });
            }

            // Wait for optimizations to take effect
            await Task.Delay(5000);

            // Get new metrics
            var newMetrics = await _metricsService.GetDeploymentMetricsAsync(deploymentId);
            
            result.MetricsAfter["cpu_usage"] = newMetrics.CPUUsagePercent;
            result.MetricsAfter["memory_usage"] = newMetrics.MemoryUsagePercent;
            result.MetricsAfter["response_time"] = newMetrics.AverageResponseTime;
            result.MetricsAfter["cache_miss_rate"] = newMetrics.CacheMissRate;

            // Calculate improvement
            var improvements = new List<double>();
            if (result.MetricsBefore["cache_miss_rate"] > 0)
            {
                improvements.Add((result.MetricsBefore["cache_miss_rate"] - result.MetricsAfter["cache_miss_rate"]) / result.MetricsBefore["cache_miss_rate"]);
            }
            if (result.MetricsBefore["response_time"] > 0)
            {
                improvements.Add((result.MetricsBefore["response_time"] - result.MetricsAfter["response_time"]) / result.MetricsBefore["response_time"]);
            }

            result.EstimatedImprovement = improvements.Count > 0 ? improvements.Average() * 100 : 0;

            return result;
        }

        public async Task<List<OptimizationRecommendation>> GetOptimizationRecommendationsAsync(Guid deploymentId)
        {
            var deployment = await _context.EdgeDeployments.FindAsync(deploymentId);
            if (deployment == null)
            {
                throw new ArgumentException($"Deployment {deploymentId} not found");
            }

            var recommendations = new List<OptimizationRecommendation>();
            var metrics = await _metricsService.GetDeploymentMetricsAsync(deploymentId);

            // Resource optimization
            if (metrics.MemoryUsagePercent < 40 && deployment.Resources.MemoryGB > 2)
            {
                recommendations.Add(new OptimizationRecommendation
                {
                    Id = Guid.NewGuid(),
                    Type = "resource",
                    Resource = "memory",
                    Description = "Memory is underutilized. Consider reducing memory allocation.",
                    Impact = 20,
                    Implementation = "Reduce memory allocation by 25%",
                    Parameters = new Dictionary<string, object> { ["new_memory_gb"] = deployment.Resources.MemoryGB * 0.75 }
                });
            }

            // Scaling optimization
            if (metrics.CPUUsagePercent > 80 && deployment.Replicas < _options.MaxReplicas)
            {
                recommendations.Add(new OptimizationRecommendation
                {
                    Id = Guid.NewGuid(),
                    Type = "scaling",
                    Resource = "replicas",
                    Description = "High CPU usage detected. Scale up to distribute load.",
                    Impact = 30,
                    Implementation = "Increase replica count",
                    Parameters = new Dictionary<string, object> { ["target_replicas"] = deployment.Replicas + 1 }
                });
            }

            // Network optimization
            if (metrics.NetworkLatency > 100)
            {
                recommendations.Add(new OptimizationRecommendation
                {
                    Id = Guid.NewGuid(),
                    Type = "network",
                    Resource = "latency",
                    Description = "High network latency detected. Consider edge caching or CDN.",
                    Impact = 25,
                    Implementation = "Enable edge caching",
                    Parameters = new Dictionary<string, object> { ["cache_enabled"] = true }
                });
            }

            return recommendations;
        }

        public async Task<bool> ApplyOptimizationAsync(Guid deploymentId, Guid recommendationId)
        {
            var recommendations = await GetOptimizationRecommendationsAsync(deploymentId);
            var recommendation = recommendations.FirstOrDefault(r => r.Id == recommendationId);
            
            if (recommendation == null)
            {
                throw new ArgumentException($"Recommendation {recommendationId} not found");
            }

            // Apply the optimization based on type
            switch (recommendation.Type)
            {
                case "resource":
                    if (recommendation.Resource == "memory" && recommendation.Parameters.ContainsKey("new_memory_gb"))
                    {
                        var newMemory = Convert.ToDouble(recommendation.Parameters["new_memory_gb"]);
                        var update = new DeploymentUpdate
                        {
                            Resources = new ResourceRequirements
                            {
                                CPU = recommendation.Parameters.ContainsKey("new_cpu") ? 
                                    Convert.ToDouble(recommendation.Parameters["new_cpu"]) : 
                                    (await _context.EdgeDeployments.FindAsync(deploymentId))!.Resources.CPU,
                                MemoryGB = newMemory,
                                StorageGB = (await _context.EdgeDeployments.FindAsync(deploymentId))!.Resources.StorageGB
                            }
                        };
                        await UpdateDeploymentAsync(deploymentId, update);
                        return true;
                    }
                    break;

                case "scaling":
                    if (recommendation.Parameters.ContainsKey("target_replicas"))
                    {
                        var targetReplicas = Convert.ToInt32(recommendation.Parameters["target_replicas"]);
                        await ScaleDeploymentAsync(deploymentId, targetReplicas);
                        return true;
                    }
                    break;

                case "network":
                    if (recommendation.Parameters.ContainsKey("cache_enabled"))
                    {
                        var config = new ConfigurationUpdate
                        {
                            ConfigMap = new Dictionary<string, string>
                            {
                                ["EDGE_CACHE_ENABLED"] = "true",
                                ["EDGE_CACHE_TTL"] = "3600"
                            }
                        };
                        var result = await UpdateConfigurationAsync(deploymentId, config);
                        return result.Success;
                    }
                    break;
            }

            return false;
        }

        #endregion

        #region Helper Methods

        private async Task<EdgeNodeEntity?> FindBestNodeForDeployment(ResourceRequirements resources)
        {
            var nodes = await _context.EdgeNodes
                .Where(n => n.Status == NodeStatus.Ready)
                .ToListAsync();

            return nodes
                .Where(n => HasSufficientResources(n, resources))
                .OrderByDescending(n => n.AvailableCapacity.CPU)
                .FirstOrDefault();
        }

        private bool HasSufficientResources(EdgeNodeEntity node, ResourceRequirements required)
        {
            return node.AvailableCapacity.CPU >= required.CPU &&
                   node.AvailableCapacity.MemoryGB >= required.MemoryGB &&
                   node.AvailableCapacity.StorageGB >= required.StorageGB &&
                   node.AvailableCapacity.NetworkMbps >= (required.NetworkMbps ?? 0);
        }

        #endregion
    }
}