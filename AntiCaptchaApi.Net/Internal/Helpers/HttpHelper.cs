using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http; // Add missing using for IHttpClientFactory
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Internal.Converters;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Responses;
using AntiCaptchaApi.Net.Responses.Abstractions;
using Newtonsoft.Json;

namespace AntiCaptchaApi.Net.Internal.Helpers
{
internal class HttpHelper(IHttpClientFactory httpClientFactory) : IHttpHelper
{
        private static readonly List<JsonConverter> Converters = new()
        {
            new TaskResultConverter<FunCaptchaSolution>(),
            new AntiGateTaskResultConverter(),
            new TaskResultConverter<GeeTestV3Solution>(),
            new TaskResultConverter<GeeTestV4Solution>(),
            new TaskResultConverter<RecaptchaSolution>(),
            new TaskResultConverter<ImageToTextSolution>(),
            new TaskResultConverter<ImageToCoordinatesSolution>(),
            new TaskResultConverter<TurnstileSolution>(),
        };
        
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

        public async Task<T> PostAsync<T>(Uri url, string payload, CancellationToken cancellationToken)
            where T : BaseResponse, new()
        {
            var httpClient = _httpClientFactory.CreateClient();
            T response = null; // Initialize response to null
            var responseContent = string.Empty;
            
            try
            {
                using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                
                // Use using statement for HttpResponseMessage
                using var httpResponseMessage = await httpClient.PostAsync(url, content, cancellationToken);
                
                // Ensure the response was successful before attempting to read content
                // httpResponseMessage.EnsureSuccessStatusCode(); // Optionally throw on bad status codes

                responseContent = await httpResponseMessage.Content.ReadAsStringAsync();
                
                // Attempt deserialization
                response = JsonConvert.DeserializeObject<T>(responseContent, Converters.ToArray());
                
                // Handle case where deserialization might return null for valid JSON (e.g., "null")
                // or if the response structure doesn't match T perfectly but doesn't throw.
                response ??= new T(); // Ensure response is not null

                // Populate raw data regardless of deserialization success
                response.RawResponse = responseContent;
                response.RawPayload = payload;

                // If deserialization yielded a result but it indicates an API error, keep it.
                // If deserialization failed but we got here, the response object is likely partially populated or just new T().
                
                return response;
            }
            catch (JsonException jsonEx) // Catch specific deserialization errors
            {
                 response ??= new T(); // Ensure response exists
                 response.ErrorDescription = $"JSON Deserialization Error: {jsonEx.Message}";
                 response.RawResponse = responseContent; // Include raw response for debugging
                 response.RawPayload = payload;
                 response.ErrorCode = "JsonError";
                 return response;
            }
            catch (HttpRequestException httpEx) // Catch specific HTTP errors
            {
                 response ??= new T(); // Ensure response exists
                 response.ErrorDescription = $"HTTP Request Error: {httpEx.Message}";
                 response.RawResponse = responseContent;
                response.RawPayload = payload;
                // HttpRequestException doesn't have StatusCode directly. Use a generic code.
                response.ErrorCode = "HttpError";
                return response;
           }
            catch (OperationCanceledException) // Catch cancellation (TaskCanceledException or OperationCanceledException)
            {
                 response ??= new T(); // Ensure response exists
                 response.ErrorDescription = "Operation was canceled.";
                 response.RawResponse = responseContent;
                 response.RawPayload = payload;
                 response.ErrorCode = "Cancelled";
                 return response;
            }
            catch (Exception ex) // Catch broader exceptions
            {
                response ??= new T(); // Ensure response exists
                response.ErrorDescription = $"An unexpected error occurred: {ex.Message}";
                response.RawResponse = responseContent;
                response.RawPayload = payload;
                response.ErrorCode = "UnexpectedError";
                return response;
            }
        }
    }
}