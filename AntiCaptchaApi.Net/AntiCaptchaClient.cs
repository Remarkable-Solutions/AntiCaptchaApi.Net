using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Internal.Errors;
using AntiCaptchaApi.Net.Internal.Http;
using AntiCaptchaApi.Net.Internal.Json;
using AntiCaptchaApi.Net.Internal.Json.Envelopes;
using AntiCaptchaApi.Net.Internal.Pipeline;
using AntiCaptchaApi.Net.Internal.Time;
using AntiCaptchaApi.Net.Tasks;

namespace AntiCaptchaApi.Net;

/// <summary>
/// The anti-captcha.com API client. It is thread-safe; create one per API key and reuse it.
/// The API key never appears in exceptions, <see cref="ToString"/> or any other output of this library.
/// </summary>
public sealed class AntiCaptchaClient : IAntiCaptchaClient
{
    private static readonly ThreadLocal<Random> RandomSource = new(() => new Random(Guid.NewGuid().GetHashCode()));

    private readonly string _apiKey;

    /// <summary>
    /// Creates a client that uses a handler shared by all such instances (connection pooling without socket exhaustion).
    /// </summary>
    /// <param name="apiKey">The anti-captcha client key.</param>
    /// <exception cref="ArgumentException"><paramref name="apiKey"/> is null, empty or whitespace.</exception>
    public AntiCaptchaClient(string apiKey)
        : this(apiKey, CreateSharedClient(), null, SystemClock.Instance, NextRandom)
    {
        SharedHandlerInUse = SharedHandler.Instance;
    }

    /// <summary>
    /// Creates a client that sends requests through <paramref name="httpClient"/>, for example one from
    /// <c>IHttpClientFactory</c>. The client is never disposed, and its <c>Timeout</c> and <c>BaseAddress</c> are never changed;
    /// <see cref="AntiCaptchaClientOptions.BaseAddress"/> and <see cref="AntiCaptchaClientOptions.RequestTimeout"/> apply instead.
    /// </summary>
    /// <param name="apiKey">The anti-captcha client key.</param>
    /// <param name="httpClient">The HTTP client to send requests with.</param>
    /// <param name="options">Client settings, or <see langword="null"/> for the defaults.</param>
    /// <exception cref="ArgumentException"><paramref name="apiKey"/> is null, empty or whitespace, or <paramref name="options"/> is invalid.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="httpClient"/> is <see langword="null"/>.</exception>
    public AntiCaptchaClient(string apiKey, HttpClient httpClient, AntiCaptchaClientOptions? options = null)
        : this(apiKey, httpClient, options, SystemClock.Instance, NextRandom)
    {
    }

    internal AntiCaptchaClient(string apiKey, HttpClient httpClient, AntiCaptchaClientOptions? options, ISystemClock clock, Func<double> random)
    {
        // The message deliberately does not include the value: it would be the (possibly mistyped) secret.
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("The API key must not be null, empty or whitespace.", nameof(apiKey));
        }
        if (httpClient is null)
        {
            throw new ArgumentNullException(nameof(httpClient));
        }

        Options = Validate(options ?? new AntiCaptchaClientOptions());
        _apiKey = apiKey;
        Clock = clock;
        Random = random;
        Redactor = new SecretRedactor(apiKey, Enumerable.Empty<string?>());
        Transport = new ApiTransport(httpClient, Options.BaseAddress, Options.RequestTimeout);
        Pipeline = new SolvePipeline(this);
    }

    internal AntiCaptchaClientOptions Options { get; }

    internal ISystemClock Clock { get; }

    internal Func<double> Random { get; }

    internal SecretRedactor Redactor { get; }

    internal ApiTransport Transport { get; }

    internal SolvePipeline Pipeline { get; }

    /// <summary>The shared handler when the key-only constructor was used; otherwise <see langword="null"/>.</summary>
    internal HttpMessageHandler? SharedHandlerInUse { get; }

    /// <inheritdoc />
    public async Task<TSolution> SolveAsync<TSolution>(CaptchaTask<TSolution> task, SolveOptions? o = null, CancellationToken ct = default)
        where TSolution : Solution
    {
        var outcome = await RunSolveAsync(task, o, ct).ConfigureAwait(false);
        if (outcome.Failure is not null)
        {
            throw outcome.Failure;
        }
        return outcome.Result.Solution!; // A result without a failure is a success, which always has a solution.
    }

    /// <inheritdoc />
    public async Task<SolveResult<TSolution>> TrySolveAsync<TSolution>(CaptchaTask<TSolution> task, SolveOptions? o = null, CancellationToken ct = default)
        where TSolution : Solution =>
        (await RunSolveAsync(task, o, ct).ConfigureAwait(false)).Result;

    /// <inheritdoc />
    public async Task<int> CreateTaskAsync(CaptchaTask task, CreateOptions? o = null, CancellationToken ct = default)
    {
        if (task is null)
        {
            throw new ArgumentNullException(nameof(task));
        }
        var callbackUrl = o?.CallbackUrl;
        if (callbackUrl is { IsAbsoluteUri: false })
        {
            throw new ArgumentException("CallbackUrl must be an absolute URI.", nameof(o));
        }
        task.RunValidation();

        var request = new CreateTaskRequest(_apiKey, TaskSerializer.ToJsonObject(task))
        {
            LanguagePool = o?.LanguagePool,
            CallbackUrl = callbackUrl?.AbsoluteUri,
        };
        var response = await Transport.PostAsync(
            "createTask",
            request,
            AntiCaptchaJsonContext.Default.CreateTaskRequest,
            AntiCaptchaJsonContext.Default.CreateTaskResponse,
            RedactorFor(task),
            ct).ConfigureAwait(false);
        return response.TaskId;
    }

    /// <inheritdoc />
    public Task<TaskStatus<TSolution>> GetTaskResultAsync<TSolution>(int taskId, CancellationToken ct = default)
        where TSolution : Solution =>
        GetTaskResultCoreAsync<TSolution>(taskId, Redactor, ct);

    // The pipeline passes the task's redactor, so that a proxy password echoed while polling is redacted too.
    internal async Task<TaskStatus<TSolution>> GetTaskResultCoreAsync<TSolution>(int taskId, SecretRedactor redactor, CancellationToken ct)
        where TSolution : Solution
    {
        var response = await PostForTaskAsync(
            "getTaskResult",
            taskId,
            new GetTaskResultRequest(_apiKey, taskId),
            AntiCaptchaJsonContext.Default.GetTaskResultRequest,
            AntiCaptchaJsonContext.Default.GetTaskResultResponse,
            redactor,
            ct).ConfigureAwait(false);
        return TaskResultMapper.Map<TSolution>(response, taskId);
    }

    // A call about an existing task: its errors carry the task id.
    private async Task<TResponse> PostForTaskAsync<TRequest, TResponse>(
        string method,
        int taskId,
        TRequest body,
        JsonTypeInfo<TRequest> requestInfo,
        JsonTypeInfo<TResponse> responseInfo,
        SecretRedactor redactor,
        CancellationToken ct)
        where TResponse : class
    {
        try
        {
            return await Transport.PostAsync(method, body, requestInfo, responseInfo, redactor, ct).ConfigureAwait(false);
        }
        catch (AntiCaptchaException ex) when (ex.TaskId is null)
        {
            throw new AntiCaptchaException(ex.Error, taskId, 0, ex);
        }
    }

    /// <inheritdoc />
    public async Task<TSolution> WaitForResultAsync<TSolution>(int taskId, SolveOptions? o = null, CancellationToken ct = default)
        where TSolution : Solution
    {
        var options = ResolveSolveOptions(o);
        var status = await Pipeline.PollAsync<TSolution>(
            taskId,
            options,
            options.FirstPollDelay ?? TimeSpan.Zero,
            Clock.UtcNow + options.Timeout,
            Redactor,
            0,
            ct).ConfigureAwait(false);
        return status.Solution!; // PollAsync returns only ready statuses, and IsReady guarantees a solution.
    }

    /// <inheritdoc />
    public Task ReportAsync(int taskId, Report report, CancellationToken ct = default)
    {
        var method = report switch
        {
            Report.IncorrectImageCaptcha => "reportIncorrectImageCaptcha",
            Report.IncorrectRecaptcha => "reportIncorrectRecaptcha",
            Report.CorrectRecaptcha => "reportCorrectRecaptcha",
            _ => throw new ArgumentOutOfRangeException(nameof(report), report, "Undefined Report value."),
        };
        return PostForTaskAsync(
            method,
            taskId,
            new TaskActionRequest(_apiKey, taskId),
            AntiCaptchaJsonContext.Default.TaskActionRequest,
            AntiCaptchaJsonContext.Default.ActionResponse,
            Redactor,
            ct);
    }

    /// <inheritdoc />
    public Task PushAntiGateVariableAsync(int taskId, string name, object value, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("The variable name must not be null, empty or whitespace.", nameof(name));
        }
        var json = AntiGateVariableValue.ToJsonNode(value, nameof(value));
        return PostForTaskAsync(
            "pushAntiGateVariable",
            taskId,
            new PushAntiGateVariableRequest(_apiKey, taskId, name, json),
            AntiCaptchaJsonContext.Default.PushAntiGateVariableRequest,
            AntiCaptchaJsonContext.Default.ActionResponse,
            Redactor,
            ct);
    }

    /// <inheritdoc />
    public async Task<decimal> GetBalanceAsync(CancellationToken ct = default)
    {
        var response = await Transport.PostAsync(
            "getBalance",
            new ClientKeyRequest(_apiKey),
            AntiCaptchaJsonContext.Default.ClientKeyRequest,
            AntiCaptchaJsonContext.Default.GetBalanceResponse,
            Redactor,
            ct).ConfigureAwait(false);
        return response.Balance ?? throw InvalidResponse("getBalance", "the response has no balance.");
    }

    /// <inheritdoc />
    public async Task<QueueStats> GetQueueStatsAsync(QueueType q, CancellationToken ct = default)
    {
        var response = await Transport.PostAsync(
            "getQueueStats",
            new GetQueueStatsRequest((int)q),
            AntiCaptchaJsonContext.Default.GetQueueStatsRequest,
            AntiCaptchaJsonContext.Default.GetQueueStatsResponse,
            Redactor,
            ct).ConfigureAwait(false);
        if (response is not { Waiting: { } waiting, Load: { } load, Bid: { } bid, Speed: { } speed, Total: { } total })
        {
            throw InvalidResponse("getQueueStats", "the response lacks waiting, load, bid, speed or total.");
        }
        return new QueueStats(waiting, load, bid, speed, total);
    }

    /// <summary>Describes the client without revealing the API key.</summary>
    /// <returns>The type name and the base address.</returns>
    public override string ToString() => $"AntiCaptchaClient {{ BaseAddress = {Options.BaseAddress} }}";

    // API descriptions can echo the task's proxy string, so its password is redacted too.
    internal SecretRedactor RedactorFor(CaptchaTask task) => Redactor.With(task.Proxy?.Password);

    // Programmer errors (null task, invalid options or task) throw here, before any HTTP call and outside the pipeline.
    private Task<SolveOutcome<TSolution>> RunSolveAsync<TSolution>(CaptchaTask<TSolution> task, SolveOptions? o, CancellationToken ct)
        where TSolution : Solution
    {
        if (task is null)
        {
            throw new ArgumentNullException(nameof(task));
        }
        var options = ResolveSolveOptions(o);
        task.RunValidation(); // CreateTaskAsync checks CallbackUrl, also before its HTTP call.
        return Pipeline.SolveAsync(task, options, RedactorFor(task), ct);
    }

    // A successful envelope that lacks a required field. The body is not included (it could echo request data).
    private static AntiCaptchaException InvalidResponse(string method, string reason) =>
        new(new AntiCaptchaError(AntiCaptchaErrorCode.InvalidResponse, null, method + ": " + reason, null), taskId: null, attempts: 0);

    private SolveOptions ResolveSolveOptions(SolveOptions? o) =>
        o is null ? Options.DefaultSolveOptions : o.Validate(nameof(o));

    private static AntiCaptchaClientOptions Validate(AntiCaptchaClientOptions options)
    {
        if (options.BaseAddress is null || !options.BaseAddress.IsAbsoluteUri)
        {
            throw new ArgumentException("BaseAddress must be an absolute URI.", nameof(options));
        }
        if (options.RequestTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "RequestTimeout must be positive.");
        }
        if (options.DefaultSolveOptions is null)
        {
            throw new ArgumentException("DefaultSolveOptions must not be null.", nameof(options));
        }
        options.DefaultSolveOptions.Validate(nameof(options));
        return options;
    }

    // The per-request timeout is enforced by ApiTransport, so this privately owned client never times out by itself.
    private static HttpClient CreateSharedClient() =>
        new(SharedHandler.Instance, disposeHandler: false) { Timeout = System.Threading.Timeout.InfiniteTimeSpan };

    // ThreadLocal.Value is annotated nullable, but the value factory above never returns null.
    private static double NextRandom() => RandomSource.Value!.NextDouble();
}
