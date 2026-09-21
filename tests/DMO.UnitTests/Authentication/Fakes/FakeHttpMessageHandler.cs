using System.Net;
using System.Net.Http.Json;

namespace DMO.UnitTests.Authentication.Fakes;

/// <summary>
/// Test-only fake HTTP transport for the real <c>SupabaseAuthenticationService</c>.
/// </summary>
/// <remarks>
/// The fake supplies the transport only; the service under test is the real product logic.
/// No real network, no real key and no real credential is involved. Requests are captured so
/// tests can assert the exact request shape (endpoint, headers, body).
/// </remarks>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;
    private readonly List<HttpRequestMessage> _requests = [];

    /// <summary>Creates a handler that responds via <paramref name="responder"/>.</summary>
    public FakeHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
    {
        ArgumentNullException.ThrowIfNull(responder);
        _responder = responder;
    }

    /// <summary>Creates a handler that always returns <paramref name="response"/>.</summary>
    public static FakeHttpMessageHandler Returning(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return new FakeHttpMessageHandler((_, _) => Task.FromResult(response));
    }

    /// <summary>Creates a JSON response with the given status code.</summary>
    public static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, object body) =>
        new(statusCode) { Content = JsonContent.Create(body) };

    /// <summary>Requests received by this handler, in order.</summary>
    public IReadOnlyList<HttpRequestMessage> Requests => _requests;

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        _requests.Add(request);
        return _responder(request, cancellationToken);
    }
}