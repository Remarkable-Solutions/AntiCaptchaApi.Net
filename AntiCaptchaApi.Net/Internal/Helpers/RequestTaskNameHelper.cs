using System;
using System.Collections.Generic;
using AntiCaptchaApi.Net.Internal.Validation.Validators;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests;
using AntiCaptchaApi.Net.Requests.Abstractions;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces;

namespace AntiCaptchaApi.Net.Internal.Helpers;

internal static class RequestTaskNameHelper
{
    public static string GetTaskName<TRequest, TSolution>(TRequest request)
        where TRequest : ICaptchaRequest<TSolution>
        where TSolution : BaseSolution
    {
        var @switch = new Dictionary<Type, string> {
            { typeof(FunCaptchaRequest), "FunCaptchaTask" },
            { typeof(FunCaptchaProxylessRequest), "FunCaptchaTaskProxyless" },
            { typeof(GeeTestV3Request), "GeeTestTask" },
            { typeof(GeeTestV3ProxylessRequest), "GeeTestTaskProxyless" },
            { typeof(GeeTestV4Request), "GeeTestTask" },
            { typeof(GeeTestV4ProxylessRequest), "GeeTestTaskProxyless" },
            { typeof(ImageToTextRequest), "ImageToTextTask" },
            { typeof(RecaptchaV2EnterpriseProxylessRequest), "RecaptchaV2EnterpriseTaskProxyless" },
            { typeof(RecaptchaV2EnterpriseRequest), "RecaptchaV2EnterpriseTask" },
            { typeof(RecaptchaV2ProxylessRequest), "RecaptchaV2TaskProxyless" },
            { typeof(RecaptchaV2Request), "RecaptchaV2Task" },
            { typeof(RecaptchaV3Request), "RecaptchaV3TaskProxyless" },
            { typeof(RecaptchaV3EnterpriseRequest), "RecaptchaV3TaskProxyless" },
            { typeof(TurnstileCaptchaProxylessRequest), "TurnstileTaskProxyless" },
            { typeof(TurnstileCaptchaRequest), "TurnstileTask" },
            { typeof(ImageToCoordinatesRequest), "ImageToCoordinatesTask" },
            { typeof(ProsopoProxylessRequest), "ProsopoTaskProxyless" },
            { typeof(ProsopoRequest), "ProsopoTask" },
            { typeof(FriendlyCaptchaProxylessRequest), "FriendlyCaptchaTaskProxyless" },
            { typeof(FriendlyCaptchaRequest), "FriendlyCaptchaTask" }
        };
        return @switch[request.GetType()];
    }
}