# RemoteC Solution Structure

This document outlines the complete solution structure and provides the necessary commands to create all directories and initialize the projects.

## Directory Structure Creation Commands

### Root Directories
```bash
mkdir -p src/RemoteC.Api
mkdir -p src/RemoteC.Web
mkdir -p src/RemoteC.Core
mkdir -p src/RemoteC.Data
mkdir -p src/RemoteC.Shared
mkdir -p src/RemoteC.Host
mkdir -p src/RemoteC.Client
mkdir -p tests/RemoteC.Tests.Unit
mkdir -p tests/RemoteC.Tests.Integration
mkdir -p tests/RemoteC.Tests.Performance
mkdir -p database/migrations
mkdir -p database/stored-procedures
mkdir -p deployment/docker
mkdir -p deployment/kubernetes
mkdir -p deployment/azure
mkdir -p scripts
mkdir -p docs/architecture
mkdir -p docs/api
```

## Project Initialization Commands

### .NET Projects
```bash
# Create solution file
dotnet new sln -n RemoteC

# Create ASP.NET Core Web API
cd src/RemoteC.Api
dotnet new webapi --framework net8.0
cd ../..

# Create Data Layer
cd src/RemoteC.Data
dotnet new classlib --framework net8.0
cd ../..

# Create Shared Library
cd src/RemoteC.Shared
dotnet new classlib --framework net8.0
cd ../..

# Create Host Application
cd src/RemoteC.Host
dotnet new wpf --framework net8.0
cd ../..

# Create Client Application
cd src/RemoteC.Client
dotnet new avalonia.app --framework net8.0
cd ../..

# Create Test Projects
cd tests/RemoteC.Tests.Unit
dotnet new xunit --framework net8.0
cd ../..

cd tests/RemoteC.Tests.Integration
dotnet new xunit --framework net8.0
cd ../..

cd tests/RemoteC.Tests.Performance
dotnet new xunit --framework net8.0
cd ../..

# Add projects to solution
dotnet sln add src/RemoteC.Api/RemoteC.Api.csproj
dotnet sln add src/RemoteC.Data/RemoteC.Data.csproj
dotnet sln add src/RemoteC.Shared/RemoteC.Shared.csproj
dotnet sln add src/RemoteC.Host/RemoteC.Host.csproj
dotnet sln add src/RemoteC.Client/RemoteC.Client.csproj
dotnet sln add tests/RemoteC.Tests.Unit/RemoteC.Tests.Unit.csproj
dotnet sln add tests/RemoteC.Tests.Integration/RemoteC.Tests.Integration.csproj
dotnet sln add tests/RemoteC.Tests.Performance/RemoteC.Tests.Performance.csproj
```

### React Frontend
```bash
cd src/RemoteC.Web
npx create-react-app . --template typescript
npm install @mui/material @emotion/react @emotion/styled
npm install @mui/icons-material @mui/lab
npm install @microsoft/signalr
npm install @azure/msal-browser @azure/msal-react
npm install react-router-dom @types/react-router-dom
npm install @tanstack/react-query
npm install react-hook-form @hookform/resolvers yup
npm install axios
npm install @types/node
cd ../..
```

### Rust Components (Phase 2)
```bash
cd src/RemoteC.Core
cargo init --lib
cd ../..
```

## NuGet Package Installation Commands

### RemoteC.Api Packages
```bash
cd src/RemoteC.Api
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.AspNetCore.SignalR
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Microsoft.Identity.Web
dotnet add package Azure.Identity
dotnet add package Azure.Security.KeyVault.Secrets
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.ApplicationInsights
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package AutoMapper
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
dotnet add package FluentValidation.AspNetCore
dotnet add package Swashbuckle.AspNetCore
dotnet add package Polly.Extensions.Http
dotnet add package Hangfire.AspNetCore
dotnet add package Hangfire.SqlServer
dotnet add package StackExchange.Redis
dotnet add package Microsoft.ApplicationInsights.AspNetCore
cd ../../..
```

### RemoteC.Data Packages
```bash
cd src/RemoteC.Data
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.EntityFrameworkCore.Design
cd ../../..
```

### Test Project Packages
```bash
cd tests/RemoteC.Tests.Unit
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Microsoft.EntityFrameworkCore.InMemory
dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package AutoFixture
dotnet add package AutoFixture.Xunit2
cd ../../..

cd tests/RemoteC.Tests.Integration
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Microsoft.EntityFrameworkCore.InMemory
dotnet add package Testcontainers.SqlServer
dotnet add package FluentAssertions
cd ../../..

cd tests/RemoteC.Tests.Performance
dotnet add package NBomber
dotnet add package BenchmarkDotNet
dotnet add package FluentAssertions
cd ../../..
```