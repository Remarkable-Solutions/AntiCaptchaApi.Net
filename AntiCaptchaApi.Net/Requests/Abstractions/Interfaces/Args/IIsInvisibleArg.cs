namespace AntiCaptchaApi.Net.Requests.Abstractions.Interfaces.Args
{
    public interface IIsInvisibleArg : IRequestArg
    {
        public bool? IsInvisible { get; set; }
    }
}