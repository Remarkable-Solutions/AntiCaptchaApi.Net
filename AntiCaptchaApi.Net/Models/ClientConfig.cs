namespace AntiCaptchaApi.Net.Models
{
    /// <summary>
    /// Configuration settings for the <see cref="AnticaptchaClient"/>.
    /// </summary>
    public class ClientConfig
    {
        /// <summary>
        /// Gets or sets the maximum time in milliseconds to wait for a captcha task to be solved when using polling methods like <see cref="IAnticaptchaClient.WaitForTaskResultAsync{TSolution}(int, System.Threading.CancellationToken)"/>.
        /// Default is 120,000 ms (2 minutes).
        /// </summary>
        public int MaxWaitForTaskResultTimeMs { get; set; } = 120000;

        /// <summary>
        /// Gets or sets the maximum time in milliseconds for individual HTTP requests to the Anti-Captcha API.
        /// Note: If using the obsolete constructor that accepts an HttpClient, this value is only applied when the client is initially created. Use Dependency Injection for better HttpClient lifetime management.
        /// Default is 60,000 ms (1 minute).
        /// </summary>
        public int MaxHttpRequestTimeMs { get; set; } = 60000;

        /// <summary>
        /// Gets or sets the number of times the <see cref="IAnticaptchaClient.SolveCaptchaAsync{TSolution}"/> method should retry creating and waiting for a task if the task result indicates an error status.
        /// Default is 1 (meaning one initial attempt, no retries on error status). Set to 0 for no initial attempt (not recommended), 2 for one retry, etc.
        /// </summary>
        public int SolveAsyncRetries { get; set; } = 1;

        /// <summary>
        /// Gets or sets the delay in milliseconds between polling requests when checking for a task result using methods like <see cref="IAnticaptchaClient.WaitForTaskResultAsync{TSolution}(int, System.Threading.CancellationToken)"/>.
        /// Default is 1000 ms (1 second).
        /// </summary>
        public int DelayTimeBetweenCheckingTaskResultMs { get; set; } = 1000;
    }
}