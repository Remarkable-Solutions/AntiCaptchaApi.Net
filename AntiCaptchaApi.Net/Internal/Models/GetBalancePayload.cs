using AntiCaptchaApi.Net.Responses;

namespace AntiCaptchaApi.Net.Internal.Models;

internal class GetBalancePayload(string clientKey) : ClientPayload<BalanceResponse>(clientKey);