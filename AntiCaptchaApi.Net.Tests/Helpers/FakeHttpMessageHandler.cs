using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AntiCaptchaApi.Net.Tests.Helpers;

/// <summary>
/// Offline <see cref="HttpMessageHandler"/> for client tests. Responses (or exceptions) are served
/// in FIFO order; every request's URI and body are captured for later assertions.
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly object _sync = new();
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();
    private readonly List<Uri?> _requestUris = new();
    private readonly List<string> _requestBodies = new();

    /// <summary>Number of requests the handler has received.</summary>
    public int RequestCount
    {
        get
        {
            lock (_sync)
            {
                return _requestBodies.Count;
            }
        }
    }

    /// <summary>Snapshot of the URIs of the received requests, in order.</summary>
    public IReadOnlyList<Uri?> RequestUris
    {
        get
        {
            lock (_sync)
            {
                return _requestUris.ToArray();
            }
        }
    }

    /// <summary>Snapshot of the bodies of the received requests, in order (empty string when there was no content).</summary>
    public IReadOnlyList<string> RequestBodies
    {
        get
        {
            lock (_sync)
            {
                return _requestBodies.ToArray();
            }
        }
    }

    /// <summary>Queues a response factory that is invoked with the incoming request.</summary>
    public FakeHttpMessageHandler Enqueue(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        ArgumentNullException.ThrowIfNull(responder);
        lock (_sync)
        {
            _responses.Enqueue(responder);
        }

        return this;
    }

    /// <summary>Queues a JSON response with the given status code (200 by default).</summary>
    public FakeHttpMessageHandler EnqueueJson(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        ArgumentNullException.ThrowIfNull(json);
        return Enqueue(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
    }

    /// <summary>Queues an exception that is thrown when the next request arrives.</summary>
    public FakeHttpMessageHandler EnqueueException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return Enqueue(_ => throw exception);
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var body = request.Content == null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        Func<HttpRequestMessage, HttpResponseMessage> responder;
        lock (_sync)
        {
            _requestUris.Add(request.RequestUri);
            _requestBodies.Add(body);

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException(
                    $"FakeHttpMessageHandler: no queued response for request #{_requestBodies.Count} to {request.RequestUri}.");
            }

            responder = _responses.Dequeue();
        }

        var response = responder(request);
        response.RequestMessage ??= request;
        return response;
    }
}
