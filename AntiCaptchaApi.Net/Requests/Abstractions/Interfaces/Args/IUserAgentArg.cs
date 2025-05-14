namespace AntiCaptchaApi.Net.Requests.Abstractions.Interfaces.Args
{
    public interface IUserAgentArg : IRequestArg
    {
        public string UserAgent { get; set; }
    }
}