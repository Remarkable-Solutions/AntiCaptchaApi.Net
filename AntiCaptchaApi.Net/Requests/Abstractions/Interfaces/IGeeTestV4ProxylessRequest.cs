using System.Collections.Generic;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces.Args;

namespace AntiCaptchaApi.Net.Requests.Abstractions.Interfaces
{
    public interface IGeeTestV4ProxylessRequest : ICaptchaRequest<GeeTestV4Solution>, IGeeTestArgs
    {
        public Dictionary<string, string> InitParameters { get; set; }   
    }
}