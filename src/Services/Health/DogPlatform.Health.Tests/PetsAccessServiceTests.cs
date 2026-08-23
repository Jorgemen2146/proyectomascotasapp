using System.Net;
using System.Net.Http.Json;
using DogPlatform.Health.Application.Services;
using DogPlatform.Health.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DogPlatform.Health.Tests;

public sealed class PetsAccessServiceTests
{
    private static readonly Guid PetId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task Reads_health_context_and_forwards_bearer_token()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { petId = PetId, speciesId = 2, birthDate = "2025-01-01T00:00:00Z", name = "Luna" })
        });
        var service = CreateService(handler);

        var result = await service.GetAccessiblePetAsync(PetId);

        Assert.Equal(PetAccessStatus.Accessible, result.Status);
        Assert.Equal(2, result.Pet!.SpeciesId);
        Assert.Equal("Luna", result.Pet.Name);
        Assert.Equal($"api/v1/pets/{PetId:D}/health-context", handler.LastRequest!.RequestUri!.PathAndQuery.TrimStart('/'));
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);
        Assert.Equal("test-token", handler.LastRequest.Headers.Authorization.Parameter);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, PetAccessStatus.NotFound)]
    [InlineData(HttpStatusCode.Forbidden, PetAccessStatus.Forbidden)]
    [InlineData(HttpStatusCode.Unauthorized, PetAccessStatus.Unauthenticated)]
    public async Task Preserves_pets_security_status(HttpStatusCode responseStatus, PetAccessStatus expected)
    {
        var service = CreateService(new StubHandler(_ => new HttpResponseMessage(responseStatus)));
        var result = await service.GetAccessiblePetAsync(PetId);
        Assert.Equal(expected, result.Status);
    }

    [Fact]
    public async Task Maps_server_error_to_unavailable()
    {
        var service = CreateService(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        var result = await service.GetAccessiblePetAsync(PetId);
        Assert.Equal(PetAccessStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task Maps_http_timeout_to_unavailable()
    {
        var service = CreateService(new StubHandler(_ => throw new TaskCanceledException("timeout")));
        var result = await service.GetAccessiblePetAsync(PetId);
        Assert.Equal(PetAccessStatus.Unavailable, result.Status);
    }

    private static PetsAccessService CreateService(HttpMessageHandler handler)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer test-token";
        return new PetsAccessService(
            new HttpClient(handler) { BaseAddress = new Uri("http://pets.internal/") },
            new HttpContextAccessor { HttpContext = context },
            NullLogger<PetsAccessService>.Instance);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(responseFactory(request));
        }
    }
}
