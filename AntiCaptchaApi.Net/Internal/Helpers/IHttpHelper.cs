using System;
using System.Threading;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Responses.Abstractions;

namespace AntiCaptchaApi.Net.Internal.Helpers
{
    internal interface IHttpHelper
    {
        Task<T> PostAsync<T>(Uri url, string payload, CancellationToken cancellationToken)
            where T : BaseResponse, new();
    }
}