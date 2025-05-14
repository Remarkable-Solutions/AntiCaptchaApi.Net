namespace AntiCaptchaApi.Net.Requests.Abstractions.Interfaces.Args
{
    public interface IIsEnterpriseArg : IRequestArg
    {
        public bool? IsEnterprise { get; set; }
    }
}