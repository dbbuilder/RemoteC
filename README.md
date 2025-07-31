# RemoteC - Enterprise Remote Control Solution

RemoteC is an enterprise-grade remote control solution designed for secure, high-performance remote access and support. Built with modern .NET 8.0 and designed for scalability, RemoteC provides a comprehensive platform for remote desktop access, file transfer, and system management.

## 🚀 Features

### Phase 1 - Core Features (Current)
- **Remote Desktop Control**: Full remote control capabilities with screen sharing
- **File Transfer**: Secure file transfer between host and client machines
- **Session Management**: Comprehensive session tracking and management
- **Enterprise Authentication**: Azure AD B2C integration with multi-factor authentication
- **Real-time Communication**: SignalR-based real-time updates and control
- **PIN-based Quick Access**: Simplified connection mechanism for support scenarios

### Phase 2 - Performance Engine (Upcoming)
- **Rust Performance Core**: Native performance optimization for screen capture and encoding
- **Hardware Acceleration**: GPU-accelerated video encoding/decoding
- **Ultra-low Latency**: Sub-50ms latency for local network connections

### Phase 3 - Enterprise Features (Planned)
- **Advanced RBAC**: Role-based access control with granular permissions
- **Compliance**: SOC2, HIPAA, and GDPR compliance features
- **Multi-tenancy**: Full isolation between organizations
- **Audit Logging**: Comprehensive audit trail for all actions
- **Session Recording**: Record and playback remote sessions
- **Analytics Dashboard**: Real-time analytics and reporting

## 📋 Prerequisites

- .NET 8.0 SDK
- SQL Server 2019+ or Azure SQL Database
- Redis 6.0+ (for distributed caching)
- Docker (optional, for containerized deployment)
- Windows 10/11 or Windows Server 2019+ (for host application)

## 🛠️ Installation

### Quick Start with Docker

```bash
# Clone the repository
git clone https://github.com/your-org/remotec.git
cd remotec

# Run with Docker Compose
docker-compose up -d
```

### Manual Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-org/remotec.git
   cd remotec
   ```

2. **Set up the database**
   ```bash
   # Run database migrations
   dotnet ef database update -p src/RemoteC.Data -s src/RemoteC.Api
   ```

3. **Configure application settings**
   ```bash
   # Copy and edit the configuration
   cp src/RemoteC.Api/appsettings.json src/RemoteC.Api/appsettings.Development.json
   # Edit appsettings.Development.json with your configuration
   ```

4. **Build and run**
   ```bash
   # Build the solution
   dotnet build

   # Run the API
   cd src/RemoteC.Api
   dotnet run
   ```

## 🔧 Configuration

### Database Connection
Configure your SQL Server connection in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=RemoteC2Db;User Id=sa;Password=YourPassword;TrustServerCertificate=true"
  }
}
```

### Redis Configuration
```json
{
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

### Authentication

#### Development Mode (No Azure AD Required)
The application includes a development authentication mode that bypasses Azure AD:
- Automatically enabled when running in development (`npm run dev`)
- Login with any username/password (e.g., admin/admin)
- Full admin access granted
- Perfect for local development and testing

#### Production Mode (Azure AD B2C)
```json
{
  "AzureAdB2C": {
    "Instance": "https://your-tenant.b2clogin.com",
    "Domain": "your-tenant.onmicrosoft.com",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "SignUpSignInPolicyId": "B2C_1_signupsignin"
  }
}
```

## 🏗️ Architecture

RemoteC follows a clean architecture pattern with the following projects:

- **RemoteC.Api**: ASP.NET Core Web API with SignalR hubs
- **RemoteC.Client**: Cross-platform Avalonia UI client application
- **RemoteC.Host**: Windows service for the host machine
- **RemoteC.Data**: Entity Framework Core data access layer
- **RemoteC.Shared**: Shared models and interfaces
- **RemoteC.Core.Interop**: Rust FFI bindings (Phase 2)

## 🧪 Testing

Run all tests:
```bash
dotnet test
```

Run specific test categories:
```bash
# Unit tests only
dotnet test --filter Category=Unit

# Integration tests
dotnet test --filter Category=Integration

# Performance benchmarks
dotnet run -c Release --project tests/RemoteC.Tests.Performance
```

## 📊 Performance

RemoteC is designed for high performance with the following targets:

- **Latency**: <100ms on LAN (Phase 1), <50ms (Phase 2)
- **Frame Rate**: 30-60 FPS depending on quality settings
- **Concurrent Sessions**: 1,000+ per server
- **File Transfer**: 100+ MB/s on gigabit networks

## 🔒 Security

- **End-to-end Encryption**: All communication is encrypted using industry-standard protocols
- **Zero Trust Architecture**: No implicit trust, continuous verification
- **Compliance Ready**: Built with SOC2, HIPAA, and GDPR requirements in mind
- **Audit Logging**: Comprehensive audit trail for all actions
- **Role-based Access**: Granular permissions system

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with [ASP.NET Core](https://dotnet.microsoft.com/apps/aspnet)
- UI powered by [Avalonia](https://avaloniaui.net/)
- Real-time communication via [SignalR](https://dotnet.microsoft.com/apps/aspnet/signalr)
- Performance core (Phase 2) built with [Rust](https://www.rust-lang.org/)

## 📞 Support

- **Documentation**: [docs.remotec.io](https://docs.remotec.io)
- **Issues**: [GitHub Issues](https://github.com/your-org/remotec/issues)
- **Discussions**: [GitHub Discussions](https://github.com/your-org/remotec/discussions)
- **Email**: support@remotec.io

---

**RemoteC** - Enterprise Remote Control Made Simple, Secure, and Fast 🚀