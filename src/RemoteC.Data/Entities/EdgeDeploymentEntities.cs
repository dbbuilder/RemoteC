using System;
using System.Collections.Generic;
using RemoteC.Shared.Models;

namespace RemoteC.Data.Entities
{
    public class EdgeNodeEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public NodeCapacity Capacity { get; set; } = new();
        public NodeCapacity AvailableCapacity { get; set; } = new();
        public string LabelsJson { get; set; } = "{}"; // Serialized dictionary
        public NodeStatus Status { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime LastSeenAt { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Version { get; set; } = string.Empty;
        
        // Navigation properties
        public virtual ICollection<EdgeDeploymentEntity> Deployments { get; set; } = new List<EdgeDeploymentEntity>();
    }

    public class EdgeDeploymentEntity
    {
        public Guid Id { get; set; }
        public string ApplicationName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string? PreviousVersion { get; set; }
        public Guid NodeId { get; set; }
        public DeploymentStatus Status { get; set; }
        public int Replicas { get; set; }
        public string ResourcesJson { get; set; } = "{}"; // Serialized ResourceRequirements
        public string EnvironmentVariablesJson { get; set; } = "{}"; // Serialized dictionary
        public string PortsJson { get; set; } = "[]"; // Serialized int array
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string ContainerId { get; set; } = string.Empty;
        public string? LoadBalancerJson { get; set; } // Serialized LoadBalancerInfo
        public string? AutoScalePolicyJson { get; set; } // Serialized AutoScalePolicy
        public string? HealthCheckConfigJson { get; set; } // Serialized HealthCheckConfig
        
        // Navigation properties
        public virtual EdgeNodeEntity Node { get; set; } = null!;
        public virtual ICollection<ConfigurationHistoryEntity> ConfigurationHistories { get; set; } = new List<ConfigurationHistoryEntity>();
        
        // Helper properties for deserialized data
        public LoadBalancerInfo? LoadBalancer => 
            !string.IsNullOrEmpty(LoadBalancerJson) 
                ? System.Text.Json.JsonSerializer.Deserialize<LoadBalancerInfo>(LoadBalancerJson) 
                : null;
                
        public Dictionary<string, string> EnvironmentVariables =>
            !string.IsNullOrEmpty(EnvironmentVariablesJson)
                ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(EnvironmentVariablesJson) ?? new()
                : new();
                
        public ResourceRequirements? Resources =>
            !string.IsNullOrEmpty(ResourcesJson)
                ? System.Text.Json.JsonSerializer.Deserialize<ResourceRequirements>(ResourcesJson)
                : null;
                
        public int[]? Ports =>
            !string.IsNullOrEmpty(PortsJson)
                ? System.Text.Json.JsonSerializer.Deserialize<int[]>(PortsJson)
                : Array.Empty<int>();
                
        public AutoScalePolicy? AutoScalePolicy =>
            !string.IsNullOrEmpty(AutoScalePolicyJson)
                ? System.Text.Json.JsonSerializer.Deserialize<AutoScalePolicy>(AutoScalePolicyJson)
                : null;
                
        public HealthCheckConfig? HealthCheckConfig =>
            !string.IsNullOrEmpty(HealthCheckConfigJson)
                ? System.Text.Json.JsonSerializer.Deserialize<HealthCheckConfig>(HealthCheckConfigJson)
                : null;
    }

    public class ConfigurationHistoryEntity
    {
        public Guid Id { get; set; }
        public Guid DeploymentId { get; set; }
        public string ChangesJson { get; set; } = "{}"; // Serialized dictionary
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public string? Comment { get; set; }
        
        // Navigation properties
        public virtual EdgeDeploymentEntity Deployment { get; set; } = null!;
        
        // Helper property for deserialized data
        public Dictionary<string, string> Changes =>
            !string.IsNullOrEmpty(ChangesJson)
                ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(ChangesJson) ?? new()
                : new();
    }

    // Add extension methods for conversion
    public static class EdgeDeploymentEntityExtensions
    {
        public static EdgeNode ToModel(this EdgeNodeEntity entity)
        {
            return new EdgeNode
            {
                Id = entity.Id,
                Name = entity.Name,
                Location = entity.Location,
                Capacity = entity.Capacity,
                AvailableCapacity = entity.AvailableCapacity,
                Labels = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(entity.LabelsJson) ?? new(),
                Status = entity.Status,
                RegisteredAt = entity.RegisteredAt,
                LastSeenAt = entity.LastSeenAt,
                IpAddress = entity.IpAddress,
                Port = entity.Port,
                Version = entity.Version
            };
        }

        public static EdgeDeployment ToModel(this EdgeDeploymentEntity entity)
        {
            return new EdgeDeployment
            {
                Id = entity.Id,
                ApplicationName = entity.ApplicationName,
                Version = entity.Version,
                PreviousVersion = entity.PreviousVersion,
                NodeId = entity.NodeId,
                Status = entity.Status,
                Replicas = entity.Replicas,
                Resources = System.Text.Json.JsonSerializer.Deserialize<ResourceRequirements>(entity.ResourcesJson) ?? new(),
                EnvironmentVariables = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(entity.EnvironmentVariablesJson) ?? new(),
                Ports = System.Text.Json.JsonSerializer.Deserialize<int[]>(entity.PortsJson) ?? Array.Empty<int>(),
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                ContainerId = entity.ContainerId,
                LoadBalancer = string.IsNullOrEmpty(entity.LoadBalancerJson) ? null : 
                    System.Text.Json.JsonSerializer.Deserialize<LoadBalancerInfo>(entity.LoadBalancerJson)
            };
        }

        public static ConfigurationHistory ToModel(this ConfigurationHistoryEntity entity)
        {
            return new ConfigurationHistory
            {
                Id = entity.Id,
                DeploymentId = entity.DeploymentId,
                Changes = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(entity.ChangesJson) ?? new(),
                UpdatedBy = entity.UpdatedBy,
                UpdatedAt = entity.UpdatedAt,
                Comment = entity.Comment
            };
        }
    }
}