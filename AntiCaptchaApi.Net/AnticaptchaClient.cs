using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Enums;
using AntiCaptchaApi.Net.Internal;
using AntiCaptchaApi.Net.Internal.Common;
using AntiCaptchaApi.Net.Internal.Extensions;
using AntiCaptchaApi.Net.Internal.Helpers;
using AntiCaptchaApi.Net.Internal.Models;
using AntiCaptchaApi.Net.Internal.Services;
using AntiCaptchaApi.Net.Models;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces;
using AntiCaptchaApi.Net.Responses;

namespace AntiCaptchaApi.Net
{
    /// <summary>
    /// The default implementation of <see cref="IAnticaptchaClient"/>. Handles communication with the Anti-Captcha API.
    /// </summary>
    public class AnticaptchaClient : IAnticaptchaClient
    {
        /// <inheritdoc />
        public ClientConfig ClientConfig { get; private set; }

        /// <summary>
        /// Gets the Anti-Captcha API client key used for authentication.
        /// </summary>
        public string ClientKey { get; }
        
        private IAnticaptchaApi AnticaptchaApi { get; set; }

        /// <summary>
        /// Internal constructor for dependency injection scenarios.
        /// This constructor is intended for use by the dependency injection container
        /// and requires services like <see cref="IAnticaptchaApi"/> and <see cref="ClientConfig"/> to be
        /// registered via the <see cref="AnticaptchaServiceCollectionExtensions.AddAnticaptcha(Microsoft.Extensions.DependencyInjection.IServiceCollection, string, Action{ClientConfig})"/> extension method.
        /// </summary>
        /// <param name="anticaptchaApi">The underlying API communication service, resolved via DI.</param>
        /// <param name="clientKey">Your Anti-Captcha API client key, provided during service registration.</param>
        /// <param name="clientConfig">Client configuration settings, resolved via DI.</param>
        internal AnticaptchaClient(
            IAnticaptchaApi anticaptchaApi,
            string clientKey,
            ClientConfig clientConfig)
        {
            ClientConfig = clientConfig ?? new ClientConfig();
            AnticaptchaApi = anticaptchaApi;
            ClientKey = clientKey;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AnticaptchaClient"/> class
        /// for direct use without dependency injection.
        /// </summary>
        /// <remarks>
        /// This constructor manually creates internal dependencies like <see cref="HttpClient"/>,
        /// <see cref="HttpHelper"/>, and <see cref="AnticaptchaApi"/>. If you require fine-grained control
        /// over these dependencies (e.g., for advanced HttpClient configuration,
        /// mocking in tests, or integration with an existing DI container),
        /// it is recommended to use dependency injection and the internal constructor
        /// by registering services via the <see cref="AnticaptchaServiceCollectionExtensions.AddAnticaptcha(Microsoft.Extensions.DependencyInjection.IServiceCollection, string, Action{ClientConfig})"/> extension method.
        /// </remarks>
        /// <param name="clientKey">Your Anti-Captcha API client key.</param>
        /// <param name="clientConfig">Optional client configuration settings.</param>
        /// <param name="httpClient">Optional http client used for requests.</param>
        public AnticaptchaClient(string clientKey, ClientConfig clientConfig = null, HttpClient httpClient = null)
        {
            ClientKey = clientKey ?? throw new ArgumentNullException(nameof(clientKey));
            ClientConfig = clientConfig ?? new ClientConfig();

            httpClient ??= new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(ClientConfig.MaxHttpRequestTimeMs)
            };
            var httpHelper = new HttpHelper(httpClient);
            AnticaptchaApi = new AnticaptchaApi(httpHelper);
        }

        /// <inheritdoc />
        public void Configure(ClientConfig clientConfig)
        {
            ClientConfig = clientConfig ?? new ClientConfig();;
        }
        
        /// <inheritdoc />
        public async Task<GetQueueStatsResponse> GetQueueStatsAsync(QueueType queueType, CancellationToken cancellationToken = default)
        {
            var payload = new GetQueueStatsPayload((int)queueType);
            return await AnticaptchaApi.CallApiMethodAsync(ApiMethod.GetQueueStats, payload, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<GetAppStatsResponse> GetAppStatsAsync(int softId, AppStatsMode? mode = null, CancellationToken cancellationToken = default)
        {
            var payload = new GetAppStatsPayload(ClientKey, softId, mode?.ToString());
            return await AnticaptchaApi.CallApiMethodAsync(ApiMethod.GetAppStats, payload, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<GetSpendingStatsResponse> GetSpendingStatsAsync(int? date = null, string queue = null, int? softId = null, string ip = null, CancellationToken cancellationToken = default)
        {
            var payload = new GetSpendingStatsPayload(ClientKey, queue, softId, date, ip);
            return await AnticaptchaApi.CallApiMethodAsync(ApiMethod.GetSpendingStats, payload, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<ActionResponse> PushAntiGateVariableAsync(int taskId, string name, object value, CancellationToken cancellationToken = default)
        {
            var payload = new PushAntiGateVariablePayload(ClientKey, taskId, name, value);
            return await AnticaptchaApi.CallApiMethodAsync(ApiMethod.PushAntiGateVariable, payload, cancellationToken);
        }

       /// <inheritdoc />
       public async Task<ActionResponse> ReportTaskOutcomeAsync(int taskId, ReportOutcome outcome, CancellationToken cancellationToken = default)
       {
           var apiMethod = outcome switch
           {
               ReportOutcome.IncorrectImageCaptcha => ApiMethod.ReportIncorrectImageCaptcha,
               ReportOutcome.IncorrectRecaptcha => ApiMethod.ReportIncorrectRecaptcha,
               ReportOutcome.CorrectRecaptcha => ApiMethod.ReportCorrectRecaptcha,
               ReportOutcome.IncorrectHCaptcha => ApiMethod.ReportIncorrectHCaptcha,
               _ => throw new ArgumentOutOfRangeException(nameof(outcome), $"Unsupported report outcome: {outcome}")
           };
           return await ReportCaptcha(taskId, apiMethod, cancellationToken);
       }
       
        /// <summary>
        /// Helper method to send a report action to the API.
        /// </summary>
        /// <param name="taskId">The task ID to report.</param>
        /// <param name="method">The specific API report method to call.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An <see cref="ActionResponse"/>.</returns>
        private async Task<ActionResponse> ReportCaptcha(int taskId, ApiMethod method, CancellationToken cancellationToken)
        {
            var payload = new ActionPayload(ClientKey, taskId);
            return await AnticaptchaApi.CallApiMethodAsync(method, payload, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<BalanceResponse> GetBalanceAsync(CancellationToken cancellationToken = default)
        {
            var payload = new GetBalancePayload(ClientKey);
            return await AnticaptchaApi.GetBalanceAsync(payload, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<TaskResultResponse<TSolution>> SolveCaptchaAsync<TSolution>(
            ICaptchaRequest<TSolution> request,
            string languagePool = null,
            string callbackUrl = null,
            CancellationToken cancellationToken = default)
            where TSolution : BaseSolution, new()
        {
            var createTaskResponse = new CreateTaskResponse();
            var taskResult = new TaskResultResponse<TSolution>();
            for (var tries = 0; tries < ClientConfig.SolveAsyncRetries; ++tries)
            {
                createTaskResponse = await CreateCaptchaTaskAsync<TSolution>(request, languagePool, callbackUrl, cancellationToken);
                if (!createTaskResponse.IsErrorResponse && createTaskResponse.TaskId.HasValue)
                {
                    taskResult = await WaitForTaskResultAsync<TSolution>(createTaskResponse, cancellationToken);
                    taskResult.CaptchaRequest = request;
                    var status = taskResult.Status;
                    var taskStatusType = TaskStatusType.Error;
                    if (!(status.GetValueOrDefault() == taskStatusType & status.HasValue))
                        return taskResult;
                }
            }
            taskResult.CreateTaskResponse = createTaskResponse;
            if (!taskResult.CreateTaskResponse.IsErrorResponse)
                return taskResult;
            taskResult.ErrorDescription = createTaskResponse.ErrorDescription;
            taskResult.ErrorCode = createTaskResponse.ErrorCode;
            taskResult.ErrorId = createTaskResponse.ErrorId;
            return taskResult;
        }

        public async Task<TaskResultResponse<ProsopoSolution>> SolveProsopoProxylessAsync(
            IProsopoProxylessRequest request,
            string languagePool = null,
            string callbackUrl = null,
            CancellationToken cancellationToken = default)
        {
            return await SolveCaptchaAsync<ProsopoSolution>(request, languagePool, callbackUrl, cancellationToken);
        }

        public async Task<TaskResultResponse<ProsopoSolution>> SolveProsopoAsync(
            IProsopoRequest request,
            string languagePool = null,
            string callbackUrl = null,
            CancellationToken cancellationToken = default)
        {
            return await SolveCaptchaAsync<ProsopoSolution>(request, languagePool, callbackUrl, cancellationToken);
        }

        public async Task<TaskResultResponse<ProsopoSolution>> SolveFriendlyCaptchaProxylessAsync(
            IFriendlyCaptchaProxylessRequest request,
            string languagePool = null,
            string callbackUrl = null,
            CancellationToken cancellationToken = default)
        {
            return await SolveCaptchaAsync<ProsopoSolution>(request, languagePool, callbackUrl, cancellationToken);
        }

        public async Task<TaskResultResponse<ProsopoSolution>> SolveFriendlyCaptchaAsync(
            IFriendlyCaptchaRequest request,
            string languagePool = null,
            string callbackUrl = null,
            CancellationToken cancellationToken = default)
        {
            return await SolveCaptchaAsync<ProsopoSolution>(request, languagePool, callbackUrl, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<CreateTaskResponse> CreateCaptchaTaskAsync<T>(ICaptchaRequest<T> request, string languagePool = null, string callbackUrl = null, CancellationToken cancellationToken = default)
            where T : BaseSolution
        {
            var validationResult = CreateCaptchaRequestHelper.Validate(request);
            if (!validationResult.IsValid)
                return new CreateTaskResponse(HttpStatusCode.BadRequest.ToString(), string.Join('\n', validationResult.Errors.Select(x => x.ToString())));

            var taskPayload = CreateCaptchaRequestHelper.Build(request);

            if (taskPayload == null)
                return new CreateTaskResponse(HttpStatusCode.BadRequest.ToString(), ErrorMessages.AnticaptchaPayloadBuildValidationFailedError);

            var payload = new CreateTaskPayload(ClientKey, taskPayload, languagePool, callbackUrl);
            return await AnticaptchaApi.CreateTaskAsync(payload, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<TaskResultResponse<TSolution>> GetTaskResultAsync<TSolution>(int taskId, CancellationToken cancellationToken)
            where TSolution : BaseSolution, new()
        {
            var payload = new GetTaskPayload<TSolution>(ClientKey, taskId);
            return await AnticaptchaApi.GetTaskResultAsync(payload, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<TaskResultResponse<TSolution>> WaitForTaskResultAsync<TSolution>(int taskId, CancellationToken cancellationToken = default(CancellationToken)) where TSolution : BaseSolution, new()
        {
            var taskResultResponse = await WaitForTaskResultAsync<TSolution>(new CreateTaskResponse()
            {
                TaskId = taskId
            }, cancellationToken);
            return taskResultResponse;
        }

        /// <summary>
        /// Waits for a task created via <see cref="CreateTaskResponse"/> to complete by polling the API.
        /// This overload is typically used internally by <see cref="SolveCaptchaAsync{TSolution}"/>.
        /// </summary>
        /// <typeparam name="TSolution">The expected solution type.</typeparam>
        /// <param name="createTaskResponse">The response object from the task creation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="TaskResultResponse{TSolution}"/> with the result or error.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="createTaskResponse"/> or its TaskId is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if an unexpected <see cref="TaskStatusType"/> is encountered during polling (should not happen).</exception>
        public async Task<TaskResultResponse<TSolution>> WaitForTaskResultAsync<TSolution>(
            CreateTaskResponse createTaskResponse,
            CancellationToken cancellationToken = default)
            where TSolution : BaseSolution, new()
        {
            if (createTaskResponse?.TaskId == null) // Simplified null check
                throw new ArgumentNullException(nameof(createTaskResponse.TaskId));
            var timer = new Stopwatch();
            timer.Start();
            var taskResultResponse = new TaskResultResponse<TSolution>()
            {
                CreateTaskResponse = createTaskResponse
            };

            while (timer.ElapsedMilliseconds <= ClientConfig.MaxWaitForTaskResultTimeMs)
            {
                await Task.Delay(ClientConfig.DelayTimeBetweenCheckingTaskResultMs, cancellationToken);
                taskResultResponse = await GetTaskResultAsync<TSolution>(createTaskResponse.TaskId.Value, cancellationToken);
                taskResultResponse.CreateTaskResponse = createTaskResponse;
                var status = taskResultResponse.Status;

                // If status is null, it indicates an unexpected response format or error not captured earlier.
                if (!status.HasValue)
                {
                    // Ensure error details are set if not already present from GetTaskResultAsync
                    taskResultResponse.ErrorCode ??= HttpStatusCode.InternalServerError.ToString();
                    taskResultResponse.ErrorDescription ??= "Received an unexpected null status from the API.";
                    return taskResultResponse;
                }
                
                switch (status.Value) // Use status.Value now that we know it HasValue
                {
                    case TaskStatusType.Processing:
                        continue;
                    case TaskStatusType.Ready:
                    case TaskStatusType.Error:
                        return taskResultResponse;
                    default:
                        // This case should ideally be unreachable if TaskStatusType enum covers all API possibilities
                        throw new ArgumentOutOfRangeException(nameof(status), $"Unexpected task status encountered: {status.Value}");
                }
            }
            
            // Timeout handling
            if (taskResultResponse.ErrorDescription == null)
            {
                 taskResultResponse.ErrorDescription = $"Anticaptcha task {createTaskResponse.TaskId} did not finish within the configured timeout ({ClientConfig.MaxWaitForTaskResultTimeMs} ms).";
            }
            if (taskResultResponse.ErrorCode == null)
            {
                taskResultResponse.ErrorCode = HttpStatusCode.RequestTimeout.ToString();
            }
            return taskResultResponse;
        }

    }
}