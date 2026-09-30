namespace AntiCaptchaApi.Net;

/// <summary>The load of one worker queue, as returned by <see cref="IAntiCaptchaClient.GetQueueStatsAsync"/> (cached by the API for 10 s).</summary>
/// <param name="Waiting">The number of idle workers online, waiting for a task.</param>
/// <param name="Load">The queue load as a percentage.</param>
/// <param name="Bid">The average task cost in USD.</param>
/// <param name="Speed">The average solve time in seconds.</param>
/// <param name="Total">The total number of workers.</param>
public sealed record QueueStats(int Waiting, double Load, decimal Bid, double Speed, int Total);
