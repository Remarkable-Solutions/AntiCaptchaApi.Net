namespace AntiCaptchaApi.Net.Enums
{
    public enum QueueType
    {
        ImageToTextEng = 1,
        ImageToTextRus = 2,
        RecaptchaB2WithProxy = 5,
        RecaptchaB2WithoutProxy = 6,
        FuncaptchaWithProxy = 7,
        FuncaptchaWithoutProxy = 10,
        GeeTestWithProxy = 12,
        GeeTestWithoutProxy = 13,
        RecaptchaV3s03 = 18,
        RecaptchaV3s07 = 19,
        RecaptchaV3s09 = 20,
        RecaptchaEnterpriseV2WithProxy = 23,
        RecaptchaEnterpriseV2WithoutProxy = 24,
        AntiGateTask  = 25,
    }
}