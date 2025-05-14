namespace AntiCaptchaApi.Net.Requests.Abstractions.Interfaces.Args
{
    public interface IGeeTestArgs : IRequestArg
    {
        public string Gt { get; set; }
        public string Challenge { get; set; }
        public string GeetestApiServerSubdomain { get; set; }
        public string GeetestGetLib { get; set; }
    }
}