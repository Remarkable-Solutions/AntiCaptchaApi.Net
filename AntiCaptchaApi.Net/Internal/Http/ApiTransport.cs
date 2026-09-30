using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Internal.Errors;
using AntiCaptchaApi.Net.Internal.Json;
using AntiCaptchaApi.Net.Internal.Json.Envelopes;

namespace AntiCaptchaApi.Net.Internal.Http;

/// <summary>
/// Sends one API call: <c>POST {BaseAddress}{method}</c> with a JSON body, bounded by the per-request timeout.
/// Every failure becomes an <see cref="AntiCaptchaException"/> with a typed error and a redacted description,
/// except cancellation of the caller's token, which always escapes as <see cref="OperationCanceledException"/>.
/// Response bodies never appear in errors.
/// </summary>
internal sealed class ApiTransport
{
    private const string JsonMediaType = "application/json";

    private readonly HttpClient _httpClient;
    private readonly Uri _baseAddress;
    private readonly TimeSpan _requestTimeout;

    public ApiTransport(HttpClient httpClient, Uri baseAddress, TimeSpan requestTimeout)
    {
        _httpClient = httpClient;
        _baseAddress = WithTrailingSlash(baseAddress);
        _requestTimeout = requestTimeout;
    }

    public async Task<TResponse> PostAsync<TRequest, TResponse>(
        string method,
        TRequest body,
        JsonTypeInfo<TRequest> requestInfo,
        JsonTypeInfo<TResponse> responseInfo,
        SecretRedactor redactor,
        CancellationToken ct)
        where TResponse : class
    {
        ct.ThrowIfCancellationRequested();
        var json = await SendAsync(method, JsonSerializer.Serialize(body, requestInfo), redactor, ct).ConfigureAwait(false);
        return Parse(method, json, responseInfo, redactor);
    }

    private async Task<string> SendAsync(string method, string requestJson, SecretRedactor redactor, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(_requestTimeout);
        try
        {
            using var content = new StringContent(requestJson, Encoding.UTF8, JsonMediaType);
            using var response = await _httpClient.PostAsync(new Uri(_baseAddress, method), content, timeout.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var status = (int)response.StatusCode;
                throw Failure(ErrorClassifier.HttpError(status,
                    $"{method}: HTTP {status.ToString(CultureInfo.InvariantCulture)} {redactor.Redact(response.ReasonPhrase)}".TrimEnd()));
            }
#if NET5_0_OR_GREATER
            return await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
#else
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            // Our per-request timeout or HttpClient.Timeout fired; the caller did not cancel.
            throw Failure(new AntiCaptchaError(AntiCaptchaErrorCode.HttpTimeout, null,
                $"{method}: the request timed out (RequestTimeout {_requestTimeout.TotalSeconds.ToString(CultureInfo.InvariantCulture)} s or HttpClient.Timeout).", null));
        }
        catch (HttpRequestException ex)
        {
            // The inner exception is not attached: its message is not under our control and could echo secrets.
            throw Failure(ErrorClassifier.HttpError(null, $"{method}: network failure: {redactor.Redact(ex.Message)}"));
        }
    }

    private static TResponse Parse<TResponse>(string method, string json, JsonTypeInfo<TResponse> responseInfo, SecretRedactor redactor)
        where TResponse : class
    {
        ApiErrorEnvelope? envelope;
        TResponse? response;
        try
        {
            envelope = JsonSerializer.Deserialize(json, AntiCaptchaJsonContext.Default.ApiErrorEnvelope);
            response = envelope is { ErrorId: 0 } ? JsonSerializer.Deserialize(json, responseInfo) : null;
        }
        catch (JsonException)
        {
            throw InvalidResponse(method);
        }

        if (envelope is null)
        {
            throw InvalidResponse(method);
        }
        if (envelope.ErrorId != 0)
        {
            // The code is mapped from the original string; an unknown raw code is kept, so it is redacted like the description.
            var error = ErrorCodeParser.FromApi(envelope.ErrorId, envelope.ErrorCode, envelope.ErrorDescription);
            throw Failure(error with { RawCode = redactor.Redact(error.RawCode), Description = redactor.Redact(error.Description) });
        }
        return response ?? throw InvalidResponse(method);
    }

    // The body is deliberately left out: it can be large, HTML, or echo request data.
    private static AntiCaptchaException InvalidResponse(string method) =>
        Failure(new AntiCaptchaError(AntiCaptchaErrorCode.InvalidResponse, null, $"{method}: the API response is not valid JSON of the expected shape.", null));

    private static AntiCaptchaException Failure(AntiCaptchaError error) => new(error, taskId: null, attempts: 0);

    private static Uri WithTrailingSlash(Uri baseAddress) =>
        baseAddress.AbsoluteUri.EndsWith("/", StringComparison.Ordinal) ? baseAddress : new Uri(baseAddress.AbsoluteUri + "/");
}
