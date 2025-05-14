using System.Threading;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Enums;
using AntiCaptchaApi.Net.Internal.Models;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Responses;
using AntiCaptchaApi.Net.Responses.Abstractions;

namespace AntiCaptchaApi.Net.Internal.Services
{
    internal interface IAnticaptchaApi
    {
        Task<CreateTaskResponse> CreateTaskAsync<TPayload>(
            TPayload payload,
            CancellationToken cancellationToken)
            where TPayload : Payload<CreateTaskResponse>;

        Task<TaskResultResponse<TSolution>> GetTaskResultAsync<TSolution>(
            GetTaskPayload<TSolution> payload,
            CancellationToken cancellationToken)
            where TSolution : BaseSolution, new();

        Task<BalanceResponse> GetBalanceAsync<TPayload>(
            TPayload payload,
            CancellationToken cancellationToken)
            where TPayload : Payload<BalanceResponse>;

        Task<TResponse> CallApiMethodAsync<TResponse>(
            ApiMethod methodName,
            Payload<TResponse> payload,
            CancellationToken cancellationToken)
            where TResponse : BaseResponse, new();
    }
}