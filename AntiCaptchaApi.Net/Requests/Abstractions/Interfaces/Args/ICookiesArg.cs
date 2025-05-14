namespace AntiCaptchaApi.Net.Requests.Abstractions.Interfaces.Args
{
    public interface ICookiesArg : IRequestArg
    {
        public string Cookies { get; set; }   
    }
}