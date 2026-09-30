using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Infrastructure;

/// <summary>A request seen by <see cref="FakeHttpMessageHandler"/>: the API method (last URI segment) and the raw body.</summary>
internal sealed record RecordedRequest(string Method, string? Body)
{
    public JsonNode? Json => Body is null ? null : JsonNode.Parse(Body);
}

/// <summary>
/// Scripted HTTP handler. Each API method (last path segment, e.g. <c>createTask</c>) has a queue of responders that
/// are consumed in order. Every request is recorded. A call without a remaining scripted response throws
/// <see cref="InvalidOperationException"/>, so tests fail loudly on unexpected traffic.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>> _scripts = new(StringComparer.Ordinal);
    private readonly List<RecordedRequest> _requests = new();

    public IReadOnlyList<RecordedRequest> Requests
    {
        get
        {
            lock (_gate)
            {
                return _requests.ToList();
            }
        }
    }

    public IReadOnlyList<RecordedRequest> RequestsFor(string method) => Requests.Where(r => r.Method == method).ToList();

    public FakeHttpMessageHandler Script(string method, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
    {
        lock (_gate)
        {
            if (!_scripts.TryGetValue(method, out var queue))
            {
                queue = new Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>();
                _scripts[method] = queue;
            }
            queue.Enqueue(responder);
        }
        return this;
    }

    public FakeHttpMessageHandler Script(string method, Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) =>
        Script(method, (request, _) => responder(request));

    public FakeHttpMessageHandler RespondJson(string method, string json, HttpStatusCode status = HttpStatusCode.OK) =>
        Script(method, (_, _) => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        }));

    public FakeHttpMessageHandler RespondStatus(string method, HttpStatusCode status) =>
        Script(method, (_, _) => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(string.Empty) }));

    public FakeHttpMessageHandler Throw(string method, Exception exception) =>
        Script(method, (_, _) => Task.FromException<HttpResponseMessage>(exception));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var method = request.RequestUri?.Segments.LastOrDefault()?.Trim('/') ?? string.Empty;
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder;
        lock (_gate)
        {
            _requests.Add(new RecordedRequest(method, body));
            if (!_scripts.TryGetValue(method, out var queue) || queue.Count == 0)
            {
                throw new InvalidOperationException($"FakeHttpMessageHandler: unscripted call to '{method}'.");
            }
            responder = queue.Dequeue();
        }

        return await responder(request, cancellationToken).ConfigureAwait(false);
    }
}
