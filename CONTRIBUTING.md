# Contributing to RemoteC

Thank you for your interest in contributing to RemoteC! This document provides guidelines and instructions for contributing to the project.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [How to Contribute](#how-to-contribute)
- [Development Setup](#development-setup)
- [Coding Standards](#coding-standards)
- [Testing Guidelines](#testing-guidelines)
- [Pull Request Process](#pull-request-process)
- [Issue Guidelines](#issue-guidelines)

## Code of Conduct

By participating in this project, you agree to abide by our Code of Conduct:

- Be respectful and inclusive
- Welcome newcomers and help them get started
- Focus on constructive criticism
- Accept feedback gracefully
- Prioritize the project's best interests

## Getting Started

1. Fork the repository on GitHub
2. Clone your fork locally:
   ```bash
   git clone https://github.com/your-username/remotec.git
   cd remotec
   ```
3. Add the upstream repository as a remote:
   ```bash
   git remote add upstream https://github.com/original-org/remotec.git
   ```
4. Create a new branch for your feature or bug fix:
   ```bash
   git checkout -b feature/your-feature-name
   ```

## How to Contribute

### Reporting Bugs

- Check if the bug has already been reported in [Issues](https://github.com/your-org/remotec/issues)
- If not, create a new issue with:
  - Clear, descriptive title
  - Steps to reproduce
  - Expected vs actual behavior
  - System information (OS, .NET version, etc.)
  - Relevant logs or screenshots

### Suggesting Features

- Check existing [Issues](https://github.com/your-org/remotec/issues) and [Discussions](https://github.com/your-org/remotec/discussions)
- Create a new discussion or issue with:
  - Clear use case
  - Proposed solution
  - Alternative solutions considered
  - Potential impact on existing features

### Code Contributions

1. **Small Changes**: For typos, documentation updates, or minor bug fixes, feel free to submit a PR directly
2. **Large Changes**: Please discuss in an issue or discussion first to ensure alignment with project goals

## Development Setup

### Prerequisites

- .NET 8.0 SDK
- Docker Desktop (for containerized development)
- Visual Studio 2022 / VS Code / Rider
- SQL Server 2019+ (or use Docker)
- Redis (or use Docker)
- Git

### Local Development

1. **Using Docker Compose (Recommended)**:
   ```bash
   docker-compose up -d
   ```

2. **Manual Setup**:
   ```bash
   # Install dependencies
   dotnet restore
   
   # Run database migrations
   dotnet ef database update -p src/RemoteC.Data -s src/RemoteC.Api
   
   # Run the API
   cd src/RemoteC.Api
   dotnet run
   ```

3. **Run Tests**:
   ```bash
   # All tests
   dotnet test
   
   # Unit tests only
   dotnet test --filter Category=Unit
   
   # With coverage
   dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
   ```

## Coding Standards

### C# Code Style

- Follow the [.editorconfig](.editorconfig) settings
- Use meaningful variable and method names
- Keep methods small and focused (Single Responsibility Principle)
- Add XML documentation to public APIs
- Handle exceptions appropriately
- Use async/await for I/O operations

### Database

- All database access MUST use stored procedures
- Follow naming convention: `sp_EntityAction` (e.g., `sp_UserCreate`)
- Include proper error handling in stored procedures
- Document complex queries

### Git Commit Messages

Follow the conventional commits format:

```
type(scope): subject

body

footer
```

Types:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `test`: Test additions or changes
- `chore`: Build process or auxiliary tool changes

Example:
```
feat(auth): add multi-factor authentication support

- Implement TOTP-based 2FA
- Add QR code generation for authenticator apps
- Update user settings UI

Closes #123
```

## Testing Guidelines

### Test Requirements

- Write tests for all new features
- Maintain or improve code coverage (target: 80%)
- Follow the Arrange-Act-Assert pattern
- Use descriptive test names that explain what is being tested

### Test Categories

- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test component interactions
- **E2E Tests**: Test complete user scenarios

Example test:
```csharp
[Fact]
[Category("Unit")]
public async Task CreateSession_WithValidRequest_ReturnsNewSession()
{
    // Arrange
    var request = new CreateSessionRequest { /* ... */ };
    
    // Act
    var result = await _service.CreateSessionAsync(request);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(SessionStatus.Pending, result.Status);
}
```

## Pull Request Process

1. **Before Submitting**:
   - Ensure all tests pass
   - Run code analysis: `dotnet build /p:AnalysisMode=AllEnabledByDefault`
   - Update documentation if needed
   - Add/update tests for your changes
   - Rebase on latest main branch

2. **PR Title and Description**:
   - Use a clear, descriptive title
   - Reference related issues (e.g., "Fixes #123")
   - Describe what changes were made and why
   - Include screenshots for UI changes
   - List any breaking changes

3. **Review Process**:
   - At least one maintainer review required
   - Address review feedback promptly
   - Keep discussions focused and professional
   - Squash commits before merging if requested

4. **After Merging**:
   - Delete your feature branch
   - Update your local main branch
   - Close related issues

## Issue Guidelines

### Creating Issues

Use issue templates when available. Include:

- **Bug Reports**:
  - Environment details
  - Steps to reproduce
  - Expected vs actual behavior
  - Error messages/logs
  - Screenshots if applicable

- **Feature Requests**:
  - Use case description
  - Proposed solution
  - Alternatives considered
  - Mockups/diagrams if applicable

### Working on Issues

- Comment on the issue to claim it
- Ask questions if requirements are unclear
- Provide regular updates on progress
- Link your PR to the issue

## Additional Resources

- [Project Documentation](https://docs.remotec.io)
- [Architecture Overview](docs/architecture.md)
- [API Documentation](https://api.remotec.io/swagger)
- [Security Policy](SECURITY.md)

## Questions?

- Check the [FAQ](https://github.com/your-org/remotec/wiki/FAQ)
- Ask in [Discussions](https://github.com/your-org/remotec/discussions)
- Contact the maintainers

Thank you for contributing to RemoteC! 🚀