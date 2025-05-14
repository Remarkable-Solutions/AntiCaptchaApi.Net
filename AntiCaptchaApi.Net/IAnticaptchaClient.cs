using System.Threading;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Enums;
using AntiCaptchaApi.Net.Models;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces;
using AntiCaptchaApi.Net.Responses;

namespace AntiCaptchaApi.Net;

/// <summary>
/// Defines the contract for interacting with the Anti-Captcha API service.
/// Provides methods for retrieving account information, creating captcha tasks, retrieving solutions, and reporting results.
/// </summary>
public interface IAnticaptchaClient
{
    /// <summary>
    /// Gets the current client configuration settings.
    /// </summary>
    ClientConfig ClientConfig { get; }

    /// <summary>
    /// Updates the client configuration settings.
    /// Note: Some settings (like HttpClient timeout) might require recreating the client instance if changed after initial construction.
    /// </summary>
    /// <param name="clientConfig">The new client configuration.</param>
    void Configure(ClientConfig clientConfig);

    /// <summary>
    /// Retrieves statistics about the specified Anti-Captcha task queue.
    /// </summary>
    /// <param name="queueType">The type of queue to get statistics for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="GetQueueStatsResponse"/> containing queue statistics.</returns>
    Task<GetQueueStatsResponse> GetQueueStatsAsync(
      QueueType queueType,
      CancellationToken cancellationToken = default (CancellationToken));

    /// <summary>
    /// Retrieves application statistics. Requires your application's SoftID.
    /// </summary>
    /// <param name="softId">Your application's SoftID.</param>
    /// <param name="mode">Optional mode for statistics (e.g., errors).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="GetAppStatsResponse"/> containing application statistics.</returns>
    Task<GetAppStatsResponse> GetAppStatsAsync(
      int softId,
      AppStatsMode? mode = null,
      CancellationToken cancellationToken = default (CancellationToken));

    /// <summary>
    /// Retrieves spending statistics for your account.
    /// </summary>
    /// <param name="date">Optional date filter (YYYY-MM-DD).</param>
    /// <param name="queue">Optional queue name filter.</param>
    /// <param name="softId">Optional SoftID filter.</param>
    /// <param name="ip">Optional IP address filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="GetSpendingStatsResponse"/> containing spending statistics.</returns>
    Task<GetSpendingStatsResponse> GetSpendingStatsAsync(
      int? date = null,
      string queue = null,
      int? softId = null,
      string ip = null,
      CancellationToken cancellationToken = default (CancellationToken));

    /// <summary>
    /// Pushes a variable value to an AntiGate task. Used for custom tasks involving AntiGate.
    /// </summary>
    /// <param name="taskId">The ID of the AntiGate task.</param>
    /// <param name="name">The name of the variable.</param>
    /// <param name="value">The value of the variable.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An <see cref="ActionResponse"/> indicating the status of the push operation.</returns>
    Task<ActionResponse> PushAntiGateVariableAsync(
      int taskId,
      string name,
      object value,
      CancellationToken cancellationToken = default (CancellationToken));

   /// <summary>
   /// Reports the outcome of a solved captcha task.
   /// Use this to provide feedback to the Anti-Captcha service, which can improve service quality and potentially grant bonuses.
   /// </summary>
   /// <param name="taskId">The ID of the task to report.</param>
   /// <param name="outcome">The outcome of the task (e.g., correct, incorrect).</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>An <see cref="ActionResponse"/> indicating the status of the report operation.</returns>
   Task<ActionResponse> ReportTaskOutcomeAsync(int taskId, ReportOutcome outcome, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current balance of your Anti-Captcha account.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="BalanceResponse"/> containing the account balance.</returns>
    Task<BalanceResponse> GetBalanceAsync(CancellationToken cancellationToken = default (CancellationToken));

    /// <summary>
    /// Creates a captcha task, waits for its completion, and returns the result in a single call.
    /// This is a convenience method that combines <see cref="CreateCaptchaTaskAsync{T}"/> and <see cref="WaitForTaskResultAsync{TSolution}(int, CancellationToken)"/>.
    /// </summary>
    /// <typeparam name="TSolution">The expected type of the captcha solution (must inherit from <see cref="BaseSolution"/>).</typeparam>
    /// <param name="request">The captcha task request details.</param>
    /// <param name="languagePool">Optional language pool for the task.</param>
    /// <param name="callbackUrl">Optional URL for receiving the task result via callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="TaskResultResponse{TSolution}"/> containing the task result or error information.</returns>
    Task<TaskResultResponse<TSolution>> SolveCaptchaAsync<TSolution>(
      ICaptchaRequest<TSolution> request,
      string languagePool = null,
      string callbackUrl = null,
      CancellationToken cancellationToken = default (CancellationToken))
      where TSolution : BaseSolution, new();

    /// <summary>
    /// Creates a Prosopo captcha task, waits for its completion, and returns the result in a single call.
    /// </summary>
    /// <param name="request">The Prosopo captcha task request details.</param>
    /// <param name="languagePool">Optional language pool for the task.</param>
    /// <param name="callbackUrl">Optional URL for receiving the task result via callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="TaskResultResponse{ProsopoSolution}"/> containing the task result or error information.</returns>
    Task<TaskResultResponse<ProsopoSolution>> SolveProsopoProxylessAsync(
      IProsopoProxylessRequest request,
      string languagePool = null,
      string callbackUrl = null,
      CancellationToken cancellationToken = default (CancellationToken));

    /// <summary>
    /// Creates a Prosopo captcha task with proxy, waits for its completion, and returns the result in a single call.
    /// </summary>
    /// <param name="request">The Prosopo captcha task request details (with proxy).</param>
    /// <param name="languagePool">Optional language pool for the task.</param>
    /// <param name="callbackUrl">Optional URL for receiving the task result via callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="TaskResultResponse{ProsopoSolution}"/> containing the task result or error information.</returns>
    Task<TaskResultResponse<ProsopoSolution>> SolveProsopoAsync(
        IProsopoRequest request,
        string languagePool = null,
        string callbackUrl = null,
        CancellationToken cancellationToken = default (CancellationToken));

    /// <summary>
    /// Creates a Friendly Captcha task (proxyless), waits for its completion, and returns the result in a single call.
    /// </summary>
    /// <param name="request">The Friendly Captcha (proxyless) task request details.</param>
    /// <param name="languagePool">Optional language pool for the task.</param>
    /// <param name="callbackUrl">Optional URL for receiving the task result via callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="TaskResultResponse{ProsopoSolution}"/> containing the task result or error information.</returns>
    Task<TaskResultResponse<ProsopoSolution>> SolveFriendlyCaptchaProxylessAsync(
        IFriendlyCaptchaProxylessRequest request,
        string languagePool = null,
        string callbackUrl = null,
        CancellationToken cancellationToken = default (CancellationToken));

    /// <summary>
    /// Creates a Friendly Captcha task (with proxy), waits for its completion, and returns the result in a single call.
    /// </summary>
    /// <param name="request">The Friendly Captcha (with proxy) task request details.</param>
    /// <param name="languagePool">Optional language pool for the task.</param>
    /// <param name="callbackUrl">Optional URL for receiving the task result via callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="TaskResultResponse{ProsopoSolution}"/> containing the task result or error information.</returns>
    Task<TaskResultResponse<ProsopoSolution>> SolveFriendlyCaptchaAsync(
        IFriendlyCaptchaRequest request,
        string languagePool = null,
        string callbackUrl = null,
        CancellationToken cancellationToken = default (CancellationToken));

    /// <summary>
    /// Creates a new captcha task on the Anti-Captcha service.
    /// This is the first step in the two-step solving process. Follow up with <see cref="GetTaskResultAsync{TSolution}"/> or <see cref="WaitForTaskResultAsync{TSolution}(int, CancellationToken)"/> to retrieve the solution.
    /// </summary>
    /// <typeparam name="T">The expected type of the captcha solution (must inherit from <see cref="BaseSolution"/>).</typeparam>
    /// <param name="request">The captcha task request details.</param>
    /// <param name="languagePool">Optional language pool for the task.</param>
    /// <param name="callbackUrl">Optional URL for receiving the task result via callback. If provided, you don't need to poll for the result.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="CreateTaskResponse"/> containing the created Task ID or error information.</returns>
    Task<CreateTaskResponse> CreateCaptchaTaskAsync<T>(
      ICaptchaRequest<T> request,
      string languagePool = null,
      string callbackUrl = null,
      CancellationToken cancellationToken = default (CancellationToken))
      where T : BaseSolution;

    /// <summary>
    /// Retrieves the result of a previously created captcha task.
    /// This method makes a single request to check the task status. The task might still be processing.
    /// Use <see cref="WaitForTaskResultAsync{TSolution}(int, CancellationToken)"/> for automatic polling.
    /// </summary>
    /// <typeparam name="TSolution">The expected type of the captcha solution (must inherit from <see cref="BaseSolution"/>).</typeparam>
    /// <param name="taskId">The ID of the task created by <see cref="CreateCaptchaTaskAsync{T}"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="TaskResultResponse{TSolution}"/> containing the task status and solution (if ready) or error information.</returns>
    Task<TaskResultResponse<TSolution>> GetTaskResultAsync<TSolution>(
      int taskId,
      CancellationToken cancellationToken)
      where TSolution : BaseSolution, new();

    /// <summary>
    /// Waits for a previously created captcha task to complete by polling the API periodically.
    /// </summary>
    /// <typeparam name="TSolution">The expected type of the captcha solution (must inherit from <see cref="BaseSolution"/>).</typeparam>
    /// <param name="taskId">The ID of the task created by <see cref="CreateCaptchaTaskAsync{T}"/>.</param>
    /// <param name="cancellationToken">Cancellation token. Also controls the overall timeout based on client configuration.</param>
    /// <returns>A <see cref="TaskResultResponse{TSolution}"/> containing the task solution or error information if the task failed or timed out.</returns>
    Task<TaskResultResponse<TSolution>> WaitForTaskResultAsync<TSolution>(
      int taskId,
      CancellationToken cancellationToken = default (CancellationToken))
      where TSolution : BaseSolution, new();
}