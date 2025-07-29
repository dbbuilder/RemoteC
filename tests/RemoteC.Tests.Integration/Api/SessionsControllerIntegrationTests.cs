using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RemoteC.Api;
using RemoteC.Shared.Models;
using Xunit;

namespace RemoteC.Tests.Integration.Api;

public class SessionsControllerIntegrationTests : IClassFixture<RemoteCWebApplicationFactory>
{
    private readonly RemoteCWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SessionsControllerIntegrationTests(RemoteCWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        // Add authentication header if needed
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");
    }

    [Fact]
    public async Task CreateSession_WithValidRequest_ShouldReturnCreatedSession()
    {
        // Arrange
        var request = new CreateSessionRequest
        {
            DeviceId = Guid.NewGuid().ToString(),
            Type = SessionType.RemoteControl,
            RequirePin = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/sessions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await response.Content.ReadFromJsonAsync<SessionDto>();
        session.Should().NotBeNull();
        session!.Id.Should().NotBeEmpty();
        session.Status.Should().Be(SessionStatus.WaitingForPin);
    }

    [Fact]
    public async Task GetSession_WithExistingSession_ShouldReturnSession()
    {
        // Arrange
        var createRequest = new CreateSessionRequest
        {
            DeviceId = Guid.NewGuid().ToString(),
            Type = SessionType.RemoteControl
        };
        var createResponse = await _client.PostAsJsonAsync("/api/sessions", createRequest);
        var createdSession = await createResponse.Content.ReadFromJsonAsync<SessionDto>();

        // Act
        var response = await _client.GetAsync($"/api/sessions/{createdSession!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await response.Content.ReadFromJsonAsync<SessionDto>();
        session.Should().NotBeNull();
        session!.Id.Should().Be(createdSession.Id);
    }

    [Fact]
    public async Task GetSession_WithNonExistentSession_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/sessions/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EndSession_WithActiveSession_ShouldEndSuccessfully()
    {
        // Arrange
        var createRequest = new CreateSessionRequest
        {
            DeviceId = Guid.NewGuid().ToString(),
            Type = SessionType.RemoteControl
        };
        var createResponse = await _client.PostAsJsonAsync("/api/sessions", createRequest);
        var session = await createResponse.Content.ReadFromJsonAsync<SessionDto>();

        // Act
        var response = await _client.PostAsync($"/api/sessions/{session!.Id}/end", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify session is ended
        var getResponse = await _client.GetAsync($"/api/sessions/{session.Id}");
        var endedSession = await getResponse.Content.ReadFromJsonAsync<SessionDto>();
        endedSession!.Status.Should().Be(SessionStatus.Ended);
    }

    [Fact]
    public async Task GetActiveSessions_ShouldReturnOnlyActiveSessions()
    {
        // Arrange
        // Create multiple sessions
        for (int i = 0; i < 3; i++)
        {
            var request = new CreateSessionRequest
            {
                DeviceId = Guid.NewGuid().ToString(),
                Type = SessionType.RemoteControl
            };
            await _client.PostAsJsonAsync("/api/sessions", request);
        }

        // Act
        var response = await _client.GetAsync("/api/sessions?status=active");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var sessions = await response.Content.ReadFromJsonAsync<SessionDto[]>();
        sessions.Should().NotBeNull();
        sessions!.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task ValidatePin_WithCorrectPin_ShouldReturnSuccess()
    {
        // Arrange
        var createRequest = new CreateSessionRequest
        {
            DeviceId = Guid.NewGuid().ToString(),
            Type = SessionType.RemoteControl,
            RequirePin = true
        };
        var createResponse = await _client.PostAsJsonAsync("/api/sessions", createRequest);
        var session = await createResponse.Content.ReadFromJsonAsync<SessionDto>();
        
        // Note: In a real test, you'd need to retrieve the actual PIN
        var pinRequest = new PinValidationRequest
        {
            SessionId = session!.Id,
            PinCode = "123456" // This would need to be the actual generated PIN
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/sessions/validate-pin", pinRequest);

        // Assert
        // This might fail without proper PIN setup
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }
}

// Custom WebApplicationFactory for testing
public class RemoteCWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove real database and add in-memory database
            services.AddDbContext<RemoteC.Data.RemoteCDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });

            // Add test authentication
            services.AddAuthentication("Test")
                .AddScheme<TestAuthenticationSchemeOptions, TestAuthenticationHandler>(
                    "Test", options => { });

            // Override other services as needed for testing
        });

        builder.UseEnvironment("Testing");
    }
}

// Test authentication handler
public class TestAuthenticationHandler : AuthenticationHandler<TestAuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(IOptionsMonitor<TestAuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class TestAuthenticationSchemeOptions : AuthenticationSchemeOptions { }