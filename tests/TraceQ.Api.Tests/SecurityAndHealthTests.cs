using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TraceQ.Core.Interfaces;
using TraceQ.Infrastructure.Data;

namespace TraceQ.Api.Tests;

public class SecurityAndHealthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SecurityAndHealthTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Remove existing DbContext registration and replace with in-memory SQLite
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<TraceQDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<TraceQDbContext>(options =>
                    options.UseSqlite("Data Source=:memory:"));

                // Replace IVectorStore with mock
                var vectorStoreDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IVectorStore));
                if (vectorStoreDescriptor != null)
                    services.Remove(vectorStoreDescriptor);

                var mockVectorStore = new Mock<IVectorStore>();
                mockVectorStore.Setup(v => v.InitializeAsync()).Returns(Task.CompletedTask);
                mockVectorStore.Setup(v => v.SearchAsync(
                        It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<Dictionary<string, string>?>()))
                    .ReturnsAsync(new List<(Guid id, float score)>());
                services.AddSingleton(mockVectorStore.Object);

                // Replace IEmbeddingService with mock
                var embeddingDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IEmbeddingService));
                if (embeddingDescriptor != null)
                    services.Remove(embeddingDescriptor);

                var mockEmbeddingService = new Mock<IEmbeddingService>();
                mockEmbeddingService.Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>()))
                    .ReturnsAsync(new float[384]);
                mockEmbeddingService.Setup(e => e.GenerateBatchEmbeddingsAsync(
                        It.IsAny<IEnumerable<(string id, string text)>>()))
                    .ReturnsAsync(new Dictionary<string, float[]>());
                services.AddSingleton(mockEmbeddingService.Object);

                // Remove Qdrant client registrations if present
                var qdrantDescriptors = services.Where(
                    d => d.ServiceType.FullName?.Contains("Qdrant") == true).ToList();
                foreach (var d in qdrantDescriptors)
                    services.Remove(d);
            });
        });
    }

    [Theory]
    [InlineData("http://evil.example.com")]
    [InlineData("http://192.168.1.100:4200")]
    [InlineData("https://attacker.io")]
    public async Task CorsPolicy_RejectsNonLocalhostOrigins(string origin)
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var request = new HttpRequestMessage(HttpMethod.Options, "/api/requirements");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await client.SendAsync(request);

        // Assert - non-allowed origins should not get an Access-Control-Allow-Origin header
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse(
            $"CORS should reject origin '{origin}' since only localhost:4200 is allowed");
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsExpectedStructure()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.ServiceUnavailable,
            (HttpStatusCode)200);

        var content = await response.Content.ReadAsStringAsync();
        // The default health check response returns "Healthy", "Degraded", or "Unhealthy" as plain text
        content.Should().NotBeNullOrEmpty();
        content.Should().BeOneOf("Healthy", "Degraded", "Unhealthy");
    }
}
