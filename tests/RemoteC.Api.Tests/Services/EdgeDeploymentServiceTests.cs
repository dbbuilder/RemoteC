using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RemoteC.Api.Services;
using RemoteC.Data;
using RemoteC.Data.Entities;
using RemoteC.Shared.Models;
using Xunit;

namespace RemoteC.Api.Tests.Services
{
    public class EdgeDeploymentServiceTests : IDisposable
    {
        private readonly RemoteCDbContext _context;
        private readonly Mock<ILogger<EdgeDeploymentService>> _loggerMock;
        private readonly Mock<IDockerService> _dockerMock;
        private readonly Mock<IKubernetesService> _k8sMock;
        private readonly Mock<IRegistryService> _registryMock;
        private readonly Mock<IMetricsService> _metricsMock;
        private readonly EdgeDeploymentService _service;
        private readonly EdgeDeploymentOptions _options;

        public EdgeDeploymentServiceTests()
        {
            // Setup in-memory database
            var dbOptions = new DbContextOptionsBuilder<RemoteCDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new RemoteCDbContext(dbOptions);

            // Setup mocks
            _loggerMock = new Mock<ILogger<EdgeDeploymentService>>();
            _dockerMock = new Mock<IDockerService>();
            _k8sMock = new Mock<IKubernetesService>();
            _registryMock = new Mock<IRegistryService>();
            _metricsMock = new Mock<IMetricsService>();

            // Setup options
            _options = new EdgeDeploymentOptions
            {
                EnableAutoScaling = true,
                EnableHealthChecks = true,
                DefaultReplicas = 3,
                MaxReplicas = 10,
                RegistryUrl = "registry.remotec.io",
                ImagePrefix = "remotec/edge",
                HealthCheckIntervalSeconds = 30,
                DeploymentTimeoutMinutes = 10
            };

            // Create service
            _service = new EdgeDeploymentService(
                _context,
                _loggerMock.Object,
                _dockerMock.Object,
                _k8sMock.Object,
                _registryMock.Object,
                _metricsMock.Object,
                Options.Create(_options));
        }

        #region Edge Node Management Tests

        [Fact]
        public async Task RegisterEdgeNodeAsync_ValidNode_CreatesSuccessfully()
        {
            // Arrange
            var nodeSpec = new EdgeNodeSpec
            {
                Name = "edge-node-01",
                Location = "us-west-1",
                Capacity = new NodeCapacity
                {
                    CPU = 8,
                    MemoryGB = 16,
                    StorageGB = 500,
                    NetworkMbps = 1000
                },
                Labels = new Dictionary<string, string>
                {
                    ["environment"] = "production",
                    ["region"] = "west"
                }
            };

            _dockerMock.Setup(d => d.ValidateNodeAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.RegisterEdgeNodeAsync(nodeSpec);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(nodeSpec.Name, result.Name);
            Assert.Equal(NodeStatus.Ready, result.Status);
            Assert.Equal(nodeSpec.Capacity.CPU, result.Capacity.CPU);
            
            // Verify in database
            var dbNode = await _context.EdgeNodes.FirstAsync();
            Assert.Equal(result.Id, dbNode.Id);
        }

        [Fact]
        public async Task RegisterEdgeNodeAsync_DuplicateName_ThrowsException()
        {
            // Arrange
            var nodeSpec = new EdgeNodeSpec
            {
                Name = "duplicate-node",
                Location = "eu-west-1"
            };

            await _service.RegisterEdgeNodeAsync(nodeSpec);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.RegisterEdgeNodeAsync(nodeSpec));
        }

        [Fact]
        public async Task GetEdgeNodeStatusAsync_ReturnsCurrentStatus()
        {
            // Arrange
            var node = await RegisterTestNode();
            
            _dockerMock.Setup(d => d.GetNodeHealthAsync(node.Id.ToString()))
                .ReturnsAsync(new NodeHealth
                {
                    Status = HealthStatus.Healthy,
                    CPUUsage = 45.5,
                    MemoryUsage = 62.3,
                    DiskUsage = 30.1,
                    NetworkLatency = 12.5
                });

            // Act
            var status = await _service.GetEdgeNodeStatusAsync(node.Id);

            // Assert
            Assert.NotNull(status);
            Assert.Equal(HealthStatus.Healthy, status.Health.Status);
            Assert.Equal(45.5, status.Health.CPUUsage);
            Assert.True(status.IsOnline);
            Assert.Equal(0, status.ActiveDeployments);
        }

        [Fact]
        public async Task UpdateEdgeNodeAsync_ModifiesNodeProperties()
        {
            // Arrange
            var node = await RegisterTestNode();
            
            var update = new EdgeNodeUpdate
            {
                Labels = new Dictionary<string, string>
                {
                    ["environment"] = "staging",
                    ["tier"] = "premium"
                },
                Capacity = new NodeCapacity
                {
                    CPU = 16,
                    MemoryGB = 32,
                    StorageGB = 1000,
                    NetworkMbps = 10000
                }
            };

            // Act
            var updated = await _service.UpdateEdgeNodeAsync(node.Id, update);

            // Assert
            Assert.Equal("staging", updated.Labels["environment"]);
            Assert.Equal("premium", updated.Labels["tier"]);
            Assert.Equal(16, updated.Capacity.CPU);
            Assert.Equal(32, updated.Capacity.MemoryGB);
        }

        [Fact]
        public async Task DecommissionNodeAsync_RemovesNodeSafely()
        {
            // Arrange
            var node = await RegisterTestNode();
            var deployment = await DeployTestApplication(node.Id);

            _dockerMock.Setup(d => d.DrainNodeAsync(node.Id.ToString()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DecommissionNodeAsync(node.Id);

            // Assert
            Assert.True(result.Success);
            Assert.Empty(result.MigratedDeployments);
            
            var dbNode = await _context.EdgeNodes.FindAsync(node.Id);
            Assert.Equal(NodeStatus.Decommissioned, dbNode!.Status);
        }

        #endregion

        #region Deployment Tests

        [Fact]
        public async Task DeployApplicationAsync_ValidDeployment_Succeeds()
        {
            // Arrange
            var node = await RegisterTestNode();
            
            var deploymentRequest = new EdgeDeploymentRequest
            {
                ApplicationName = "remotec-agent",
                Version = "1.2.3",
                NodeId = node.Id,
                Resources = new ResourceRequirements
                {
                    CPU = 2,
                    MemoryGB = 4,
                    StorageGB = 10
                },
                EnvironmentVariables = new Dictionary<string, string>
                {
                    ["API_URL"] = "https://api.remotec.io",
                    ["LOG_LEVEL"] = "info"
                },
                Ports = new[] { 8080, 9090 }
            };

            _registryMock.Setup(r => r.ImageExistsAsync($"{_options.ImagePrefix}/{deploymentRequest.ApplicationName}", deploymentRequest.Version))
                .ReturnsAsync(true);

            _dockerMock.Setup(d => d.DeployContainerAsync(It.IsAny<ContainerSpec>()))
                .ReturnsAsync(new ContainerInfo { Id = "container-123", Status = "running" });

            // Act
            var deployment = await _service.DeployApplicationAsync(deploymentRequest);

            // Assert
            Assert.NotNull(deployment);
            Assert.Equal(DeploymentStatus.Running, deployment.Status);
            Assert.Equal(deploymentRequest.ApplicationName, deployment.ApplicationName);
            Assert.Equal(deploymentRequest.Version, deployment.Version);
            Assert.Equal(node.Id, deployment.NodeId);
        }

        [Fact]
        public async Task DeployApplicationAsync_InsufficientResources_Fails()
        {
            // Arrange
            var node = await RegisterTestNode("small-node", cpu: 2, memoryGB: 4);
            
            var deploymentRequest = new EdgeDeploymentRequest
            {
                ApplicationName = "resource-heavy-app",
                Version = "1.0.0",
                NodeId = node.Id,
                Resources = new ResourceRequirements
                {
                    CPU = 8,
                    MemoryGB = 16
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.DeployApplicationAsync(deploymentRequest));
        }

        [Fact]
        public async Task UpdateDeploymentAsync_ChangesVersion()
        {
            // Arrange
            var deployment = await CreateTestDeployment();
            
            var updateRequest = new DeploymentUpdate
            {
                Version = "1.2.4",
                EnvironmentVariables = new Dictionary<string, string>
                {
                    ["LOG_LEVEL"] = "debug"
                }
            };

            _registryMock.Setup(r => r.ImageExistsAsync(It.IsAny<string>(), updateRequest.Version))
                .ReturnsAsync(true);

            _dockerMock.Setup(d => d.UpdateContainerAsync(It.IsAny<string>(), It.IsAny<ContainerSpec>()))
                .ReturnsAsync(true);

            // Act
            var updated = await _service.UpdateDeploymentAsync(deployment.Id, updateRequest);

            // Assert
            Assert.Equal("1.2.4", updated.Version);
            Assert.Equal("debug", updated.EnvironmentVariables["LOG_LEVEL"]);
            Assert.Equal(DeploymentStatus.Running, updated.Status);
        }

        [Fact]
        public async Task RollbackDeploymentAsync_RevertsToPreviousVersion()
        {
            // Arrange
            var deployment = await CreateTestDeployment(version: "2.0.0");
            deployment.PreviousVersion = "1.9.0";
            await _context.SaveChangesAsync();

            _dockerMock.Setup(d => d.UpdateContainerAsync(It.IsAny<string>(), It.IsAny<ContainerSpec>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.RollbackDeploymentAsync(deployment.Id);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("1.9.0", result.CurrentVersion);
            Assert.Equal("2.0.0", result.RolledBackFrom);
        }

        #endregion

        #region Auto-Scaling Tests

        [Fact]
        public async Task ScaleDeploymentAsync_IncreasesReplicas()
        {
            // Arrange
            var deployment = await CreateTestDeployment(replicas: 1);
            
            _dockerMock.Setup(d => d.ScaleContainerAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.ScaleDeploymentAsync(deployment.Id, 3);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(3, result.CurrentReplicas);
            Assert.Equal(1, result.PreviousReplicas);
        }

        [Fact]
        public async Task AutoScaleAsync_ScalesBasedOnMetrics()
        {
            // Arrange
            var deployment = await CreateTestDeployment(replicas: 2);
            
            // Simulate high CPU usage
            _metricsMock.Setup(m => m.GetDeploymentMetricsAsync(deployment.Id))
                .ReturnsAsync(new DeploymentMetrics
                {
                    CPUUsagePercent = 85,
                    MemoryUsagePercent = 70,
                    RequestsPerSecond = 1000
                });

            _dockerMock.Setup(d => d.ScaleContainerAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.AutoScaleAsync(deployment.Id);

            // Assert
            Assert.True(result.Scaled);
            Assert.True(result.NewReplicas > 2); // Should scale up
            Assert.Contains("CPU", result.Reason);
        }

        [Fact]
        public async Task AutoScaleAsync_RespectsMaxReplicas()
        {
            // Arrange
            var deployment = await CreateTestDeployment(replicas: _options.MaxReplicas);
            
            _metricsMock.Setup(m => m.GetDeploymentMetricsAsync(deployment.Id))
                .ReturnsAsync(new DeploymentMetrics
                {
                    CPUUsagePercent = 95,
                    MemoryUsagePercent = 90
                });

            // Act
            var result = await _service.AutoScaleAsync(deployment.Id);

            // Assert
            Assert.False(result.Scaled);
            Assert.Equal(_options.MaxReplicas, result.NewReplicas);
            Assert.Contains("maximum", result.Reason, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Health Monitoring Tests

        [Fact]
        public async Task CheckDeploymentHealthAsync_HealthyDeployment_ReturnsHealthy()
        {
            // Arrange
            var deployment = await CreateTestDeployment();
            
            _dockerMock.Setup(d => d.GetContainerHealthAsync(It.IsAny<string>()))
                .ReturnsAsync(new ContainerHealth
                {
                    Status = HealthStatus.Healthy,
                    Checks = new[]
                    {
                        new HealthCheck { Name = "http", Status = "passing" },
                        new HealthCheck { Name = "tcp", Status = "passing" }
                    }
                });

            // Act
            var health = await _service.CheckDeploymentHealthAsync(deployment.Id);

            // Assert
            Assert.Equal(HealthStatus.Healthy, health.Status);
            Assert.All(health.Checks, check => Assert.Equal("passing", check.Status));
            Assert.True(health.LastCheckTime > DateTime.UtcNow.AddMinutes(-1));
        }

        [Fact]
        public async Task CheckDeploymentHealthAsync_FailingChecks_ReturnsUnhealthy()
        {
            // Arrange
            var deployment = await CreateTestDeployment();
            
            _dockerMock.Setup(d => d.GetContainerHealthAsync(It.IsAny<string>()))
                .ReturnsAsync(new ContainerHealth
                {
                    Status = HealthStatus.Unhealthy,
                    Checks = new[]
                    {
                        new HealthCheck { Name = "http", Status = "failing", Message = "Connection refused" },
                        new HealthCheck { Name = "tcp", Status = "passing" }
                    }
                });

            // Act
            var health = await _service.CheckDeploymentHealthAsync(deployment.Id);

            // Assert
            Assert.Equal(HealthStatus.Unhealthy, health.Status);
            Assert.Contains(health.Checks, c => c.Status == "failing");
            Assert.Contains("Connection refused", health.Checks.First(c => c.Status == "failing").Message);
        }

        [Fact]
        public async Task MonitorAllDeploymentsAsync_DetectsUnhealthyDeployments()
        {
            // Arrange
            var healthyDeployment = await CreateTestDeployment("healthy-app");
            var unhealthyDeployment = await CreateTestDeployment("unhealthy-app");
            
            _dockerMock.Setup(d => d.GetContainerHealthAsync(It.IsAny<string>()))
                .ReturnsAsync((string containerId) =>
                {
                    if (containerId.Contains("healthy"))
                        return new ContainerHealth { Status = RemoteC.Shared.Models.HealthStatus.Healthy };
                    else
                        return new ContainerHealth { Status = RemoteC.Shared.Models.HealthStatus.Unhealthy };
                });

            // Act
            var report = await _service.MonitorAllDeploymentsAsync();

            // Assert
            Assert.Equal(2, report.TotalDeployments);
            Assert.Equal(1, report.HealthyDeployments);
            Assert.Equal(1, report.UnhealthyDeployments);
            Assert.Single(report.UnhealthyDeploymentIds);
            Assert.Contains(unhealthyDeployment.Id, report.UnhealthyDeploymentIds);
        }

        #endregion

        #region Load Balancing Tests

        [Fact]
        public async Task ConfigureLoadBalancerAsync_SetsUpCorrectly()
        {
            // Arrange
            var deployment = await CreateTestDeployment(replicas: 3);
            
            var lbConfig = new LoadBalancerConfig
            {
                Algorithm = RemoteC.Shared.Models.LoadBalancingAlgorithm.RoundRobin,
                HealthCheckPath = "/health",
                HealthCheckInterval = 10,
                StickySession = false
            };

            _dockerMock.Setup(d => d.ConfigureLoadBalancerAsync(It.IsAny<string>(), It.IsAny<LoadBalancerConfig>()))
                .ReturnsAsync(new LoadBalancerInfo
                {
                    Endpoint = "lb.remotec.io",
                    Port = 443,
                    BackendCount = 3
                });

            // Act
            var result = await _service.ConfigureLoadBalancerAsync(deployment.Id, lbConfig);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("lb.remotec.io", result.Endpoint);
            Assert.Equal(3, result.BackendCount);
        }

        [Fact]
        public async Task GetLoadBalancerStatsAsync_ReturnsTrafficDistribution()
        {
            // Arrange
            var deployment = await CreateTestDeployment(replicas: 3);
            
            _dockerMock.Setup(d => d.GetLoadBalancerStatsAsync(It.IsAny<string>()))
                .ReturnsAsync(new LoadBalancerStats
                {
                    TotalRequests = 10000,
                    BackendStats = new[]
                    {
                        new BackendStats { InstanceId = "1", RequestCount = 3334, HealthStatus = "healthy" },
                        new BackendStats { InstanceId = "2", RequestCount = 3333, HealthStatus = "healthy" },
                        new BackendStats { InstanceId = "3", RequestCount = 3333, HealthStatus = "healthy" }
                    }
                });

            // Act
            var stats = await _service.GetLoadBalancerStatsAsync(deployment.Id);

            // Assert
            Assert.Equal(10000, stats.TotalRequests);
            Assert.Equal(3, stats.BackendStats.Length);
            Assert.All(stats.BackendStats, b => Assert.InRange(b.RequestCount, 3300, 3400)); // Even distribution
        }

        #endregion

        #region Multi-Region Deployment Tests

        [Fact]
        public async Task DeployToMultipleRegionsAsync_DistributesDeployments()
        {
            // Arrange
            var regions = new[] { "us-west-1", "us-east-1", "eu-west-1" };
            var nodes = new List<EdgeNode>();
            
            foreach (var region in regions)
            {
                nodes.Add(await RegisterTestNode($"node-{region}", location: region));
            }

            var request = new MultiRegionDeploymentRequest
            {
                ApplicationName = "global-app",
                Version = "1.0.0",
                Regions = regions,
                ReplicasPerRegion = 2,
                Resources = new ResourceRequirements { CPU = 1, MemoryGB = 2 }
            };

            _registryMock.Setup(r => r.ImageExistsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            _dockerMock.Setup(d => d.DeployContainerAsync(It.IsAny<ContainerSpec>()))
                .ReturnsAsync(new ContainerInfo { Id = Guid.NewGuid().ToString(), Status = "running" });

            // Act
            var result = await _service.DeployToMultipleRegionsAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(3, result.RegionDeployments.Count);
            Assert.All(result.RegionDeployments, rd => Assert.Equal(2, rd.ReplicaCount));
            Assert.Equal(6, result.TotalReplicas); // 3 regions * 2 replicas
        }

        [Fact]
        public async Task FailoverDeploymentAsync_MigratesFromFailedNode()
        {
            // Arrange
            var failingNode = await RegisterTestNode("failing-node", location: "us-west-1");
            var healthyNode = await RegisterTestNode("healthy-node", location: "us-west-1");
            
            var deployment = await DeployTestApplication(failingNode.Id);
            
            _dockerMock.Setup(d => d.GetNodeHealthAsync(failingNode.Id.ToString()))
                .ReturnsAsync(new NodeHealth { Status = RemoteC.Shared.Models.HealthStatus.Unhealthy });

            _dockerMock.Setup(d => d.GetNodeHealthAsync(healthyNode.Id.ToString()))
                .ReturnsAsync(new NodeHealth { Status = RemoteC.Shared.Models.HealthStatus.Healthy });

            _dockerMock.Setup(d => d.MigrateContainerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.FailoverDeploymentAsync(deployment.Id);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(failingNode.Id, result.FromNodeId);
            Assert.Equal(healthyNode.Id, result.ToNodeId);
            Assert.True(result.MigrationTime.TotalSeconds < 60); // Quick failover
        }

        #endregion

        #region Configuration Management Tests

        [Fact]
        public async Task UpdateConfigurationAsync_PropagatesChanges()
        {
            // Arrange
            var deployment = await CreateTestDeployment();
            
            var configUpdate = new ConfigurationUpdate
            {
                ConfigMap = new Dictionary<string, string>
                {
                    ["database.host"] = "new-db.remotec.io",
                    ["cache.ttl"] = "3600"
                },
                Secrets = new Dictionary<string, string>
                {
                    ["api.key"] = "new-secret-key"
                }
            };

            _dockerMock.Setup(d => d.UpdateConfigurationAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateConfigurationAsync(deployment.Id, configUpdate);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.RestartRequired);
            Assert.NotNull(result.UpdatedAt);
        }

        [Fact]
        public async Task GetConfigurationHistoryAsync_TracksChanges()
        {
            // Arrange
            var deployment = await CreateTestDeployment();
            
            // Make multiple config updates
            for (int i = 0; i < 3; i++)
            {
                await _service.UpdateConfigurationAsync(deployment.Id, new ConfigurationUpdate
                {
                    ConfigMap = new Dictionary<string, string> { [$"key{i}"] = $"value{i}" }
                });
            }

            // Act
            var history = await _service.GetConfigurationHistoryAsync(deployment.Id);

            // Assert
            Assert.Equal(3, history.Count);
            Assert.All(history, h => Assert.NotNull(h.UpdatedAt));
            Assert.Equal("value2", history.First().Changes["key2"]);
        }

        #endregion

        #region Performance Optimization Tests

        [Fact]
        public async Task OptimizeDeploymentAsync_ImprovesCaching()
        {
            // Arrange
            var deployment = await CreateTestDeployment();
            
            _metricsMock.Setup(m => m.GetDeploymentMetricsAsync(deployment.Id))
                .ReturnsAsync(new DeploymentMetrics
                {
                    CacheMissRate = 0.6, // High miss rate
                    AverageResponseTime = 250
                });

            _dockerMock.Setup(d => d.UpdateContainerAsync(It.IsAny<string>(), It.IsAny<ContainerSpec>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.OptimizeDeploymentAsync(deployment.Id);

            // Assert
            Assert.True(result.OptimizationsApplied.Count > 0);
            Assert.Contains("cache", result.OptimizationsApplied.First().Type, StringComparison.OrdinalIgnoreCase);
            Assert.True(result.EstimatedImprovement > 0);
        }

        [Fact]
        public async Task GetOptimizationRecommendationsAsync_SuggestsImprovements()
        {
            // Arrange
            var deployment = await CreateTestDeployment();
            
            _metricsMock.Setup(m => m.GetDeploymentMetricsAsync(deployment.Id))
                .ReturnsAsync(new DeploymentMetrics
                {
                    CPUUsagePercent = 90,
                    MemoryUsagePercent = 30, // Overprovisioned memory
                    NetworkLatency = 150,
                    ErrorRate = 0.05
                });

            // Act
            var recommendations = await _service.GetOptimizationRecommendationsAsync(deployment.Id);

            // Assert
            Assert.NotEmpty(recommendations);
            Assert.Contains(recommendations, r => r.Type == "resource" && r.Resource == "memory");
            Assert.Contains(recommendations, r => r.Type == "scaling");
            Assert.All(recommendations, r => Assert.True(r.Impact > 0));
        }

        #endregion

        #region Helper Methods

        private async Task<EdgeNode> RegisterTestNode(
            string name = "test-node",
            string location = "us-west-1",
            int cpu = 8,
            int memoryGB = 16)
        {
            var nodeSpec = new EdgeNodeSpec
            {
                Name = name,
                Location = location,
                Capacity = new NodeCapacity
                {
                    CPU = cpu,
                    MemoryGB = memoryGB,
                    StorageGB = 500,
                    NetworkMbps = 1000
                }
            };

            _dockerMock.Setup(d => d.ValidateNodeAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            return await _service.RegisterEdgeNodeAsync(nodeSpec);
        }

        private async Task<EdgeDeployment> CreateTestDeployment(
            string appName = "test-app",
            string version = "1.0.0",
            int replicas = 1)
        {
            var node = await RegisterTestNode();
            
            var deployment = new EdgeDeployment
            {
                Id = Guid.NewGuid(),
                ApplicationName = appName,
                Version = version,
                NodeId = node.Id,
                Status = DeploymentStatus.Running,
                Replicas = replicas,
                Resources = new ResourceRequirements { CPU = 1, MemoryGB = 2 },
                CreatedAt = DateTime.UtcNow,
                ContainerId = $"container-{Guid.NewGuid()}"
            };

            _context.EdgeDeployments.Add(deployment);
            await _context.SaveChangesAsync();

            return deployment;
        }

        private async Task<EdgeDeployment> DeployTestApplication(Guid nodeId)
        {
            var request = new EdgeDeploymentRequest
            {
                ApplicationName = "test-app",
                Version = "1.0.0",
                NodeId = nodeId,
                Resources = new ResourceRequirements { CPU = 1, MemoryGB = 2 }
            };

            _registryMock.Setup(r => r.ImageExistsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            _dockerMock.Setup(d => d.DeployContainerAsync(It.IsAny<ContainerSpec>()))
                .ReturnsAsync(new ContainerInfo { Id = Guid.NewGuid().ToString(), Status = "running" });

            return await _service.DeployApplicationAsync(request);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        #endregion
    }
}

