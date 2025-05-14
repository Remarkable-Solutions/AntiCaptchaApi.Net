namespace AntiCaptchaApi.Net.Requests.Abstractions.Interfaces.Args
{
    public interface IWebsiteKeyArg : IRequestArg
    {
        public string WebsiteKey { get; set; }
    }
}