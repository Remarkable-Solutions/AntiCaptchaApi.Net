namespace AntiCaptchaApi.Net.Enums
{
    public enum ApiMethod
    {
        CreateTask,
        GetTaskResult,
        GetBalance,
        GetQueueStats,
        ReportIncorrectImageCaptcha,
        ReportIncorrectRecaptcha,
        ReportCorrectRecaptcha,
        ReportIncorrectHCaptcha,
        PushAntiGateVariable,
        GetSpendingStats,
        GetAppStats,
    }
}