using System.Net;
using System.Text;
using System.Text.Json;

namespace VoteService.Api.Tests;

// Lets a test decide exactly what response an outbound HttpClient call gets back,
// without touching the network. PollServiceClient and RealtimeServiceClient both
// just take a plain HttpClient in their constructor, so this handler is all we
// need to unit test the Vote Service's side of those service-to-service calls.
public class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(responder(request));
}

// Simulates the target service being completely unreachable (connection refused,
// DNS failure, timeout, etc.) - the same failure mode RealtimeServiceClient's
// try/catch is meant to survive.
public class UnavailableHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => throw new HttpRequestException("Simulated: target service is unreachable.");
}

public static class TestHttp
{
    public static HttpResponseMessage JsonResponse(object body, HttpStatusCode status = HttpStatusCode.OK) => new(status)
    {
        Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
    };
}
