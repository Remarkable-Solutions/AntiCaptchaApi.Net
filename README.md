# AntiCaptchaApi.Net [![NuGet Version](https://img.shields.io/nuget/v/AntiCaptchaApi.Net.svg)](https://www.nuget.org/packages/AntiCaptchaApi.Net/)

A .NET 9 library that implements the [anti-captcha.com API](https://anti-captcha.com/apidoc).

AntiCaptchaApi.Net provides a convenient wrapper around the Anti-captcha API methods, allowing you to easily integrate captcha solving capabilities into your .NET applications. It supports various captcha types and provides options for configuration and error handling.

## Features

*   Supports multiple captcha types (ImageToText, Recaptcha V2/V3/Enterprise, HCaptcha, FunCaptcha, GeeTest, Turnstile, etc.)
*   Async methods for non-blocking operations.
*   Configuration options for timeouts, retries, and polling intervals.
*   Dependency Injection support for easy integration.
*   Proxy support for relevant task types.
*   Task reporting capabilities.

## Installation

You can install the package via the .NET CLI or the NuGet Package Manager Console.

**.NET CLI**
```bash
dotnet add package AntiCaptchaApi.Net
```

**Package Manager Console**
```powershell
Install-Package AntiCaptchaApi.Net
```

## Migration Guide (From Older Versions or Manual HttpClient Usage)

This library now exclusively uses `IHttpClientFactory` for robust and efficient management of `HttpClient` instances, integrated via Dependency Injection (DI). Manual instantiation of `AnticaptchaClient` is no longer supported.

**Why the Change?**

Using `IHttpClientFactory` provides several benefits:
*   **Improved Performance:** Addresses issues like socket exhaustion that can occur with direct `HttpClient` instantiation and disposal.
*   **Centralized Configuration:** Allows for configuring `HttpClient` instances (e.g., default headers, Polly policies for resilience) in a central place.
*   **Simplified Lifetime Management:** `IHttpClientFactory` manages the lifetime of `HttpClientMessageHandler` instances, optimizing resource use.

**How to Migrate/Adopt:**

If you were previously instantiating `AnticaptchaClient` manually, you **must** switch to the Dependency Injection (DI) approach:

1.  **Ensure you have the `Microsoft.Extensions.Http` package referenced** (usually included with ASP.NET Core or available as a separate NuGet package). The `AddAnticaptcha` method will call `services.AddHttpClient()` for you.
2.  **Register `IAnticaptchaClient` using the `AddAnticaptcha` extension method** in your `ConfigureServices` method (e.g., in `Startup.cs` or `Program.cs`).

**Before (Example of manual instantiation - Now Obsolete):**
```csharp
// This pattern is no longer supported:
// var client = new AnticaptchaClient("YOUR_API_KEY", new ClientConfig());
```

**After (Required DI approach):**
```csharp
// In your service configuration (e.g., Startup.cs or Program.cs)
services.AddAnticaptcha("YOUR_API_KEY", options => {
    // Optional: configure client options
    options.MaxWaitForTaskResultTimeMs = 180000;
});

// In your service/controller
public class MyService
{
    private readonly IAnticaptchaClient _anticaptchaClient;

    public MyService(IAnticaptchaClient anticaptchaClient)
    {
        _anticaptchaClient = anticaptchaClient;
    }
    // ... use _anticaptchaClient
}
```
By adopting the `AddAnticaptcha` method, you ensure that the library uses `IHttpClientFactory` correctly, leading to a more stable and performant application.

## Configuration

The client behavior can be customized using `ClientConfig`.

**Using Dependency Injection (Required):**

The only way to obtain and configure an `IAnticaptchaClient` instance is through service registration using the `AddAnticaptcha` extension method. This ensures proper `HttpClient` management via `IHttpClientFactory`.

```csharp
using AntiCaptchaApi.Net;
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    // Add other services...

    string antiCaptchaApiKey = "YOUR_API_KEY"; // Replace with your actual API key

    services.AddAnticaptcha(antiCaptchaApiKey, options =>
    {
        // Customize client configuration (optional)
        options.MaxWaitForTaskResultTimeMs = 180000; // Wait up to 3 minutes for task result
        options.DelayTimeBetweenCheckingTaskResultMs = 5000; // Poll every 5 seconds
        options.SolveAsyncRetries = 2; // Retry once on solvable errors
    });

    // Add other services...
}
```

## Basic Usage (with Dependency Injection)

1.  **Register the client** in your `Startup.cs` or `Program.cs` as shown in the Configuration section.
2.  **Inject `IAnticaptchaClient`** into your service/controller.
3.  **Create a request** object for the desired captcha type.
4.  **Call `SolveCaptchaAsync`** to get the solution.

**Example: Solving an ImageToText Captcha**

```csharp
using AntiCaptchaApi.Net;
using AntiCaptchaApi.Net.Enums;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests;
using System;
using System.Threading.Tasks;

public class MyCaptchaService
{
    private readonly IAnticaptchaClient _anticaptchaClient;

    public MyCaptchaService(IAnticaptchaClient anticaptchaClient)
    {
        _anticaptchaClient = anticaptchaClient;
    }

    public async Task<string> SolveImageCaptchaAsync(string imageFilePath)
    {
        var request = new ImageToTextRequest
        {
            FilePath = imageFilePath, // Loads image from path into BodyBase64
            // Optional parameters:
            // Phrase = false,
            // Case = true,
            // Numeric = NumericOption.NumbersOnly,
            // MinLength = 4,
            // MaxLength = 6
        };

        try
        {
            // Solve the captcha (creates task and waits for result)
            var result = await _anticaptchaClient.SolveCaptchaAsync(request);

            if (result.Status == TaskStatusType.Ready)
            {
                Console.WriteLine($"Captcha solved successfully! Solution: {result.Solution.Text}");
                return result.Solution.Text;
            }
            else
            {
                Console.WriteLine($"Captcha solving failed. Error: {result.ErrorCode} - {result.ErrorDescription}");
                // Optionally report incorrect solution if applicable (though less common for ImageToText)
                // await _anticaptchaClient.ReportTaskOutcomeAsync(result.CreateTaskResponse.TaskId.Value, ReportOutcome.IncorrectImageCaptcha);
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An exception occurred: {ex.Message}");
            return null;
        }
    }
}
```

**Alternative: Two-Step Solving**

You can also create the task and poll for the result manually.

```csharp
// 1. Create the task
var createTaskResponse = await _anticaptchaClient.CreateCaptchaTaskAsync(request);

if (createTaskResponse.IsErrorResponse || !createTaskResponse.TaskId.HasValue)
{
    Console.WriteLine($"Failed to create task: {createTaskResponse.ErrorCode} - {createTaskResponse.ErrorDescription}");
    return;
}

Console.WriteLine($"Task created with ID: {createTaskResponse.TaskId.Value}");

// 2. Wait for the result (polls the API)
var taskResult = await _anticaptchaClient.WaitForTaskResultAsync<ImageToTextSolution>(createTaskResponse.TaskId.Value);

if (taskResult.Status == TaskStatusType.Ready)
{
    Console.WriteLine($"Captcha solved! Solution: {taskResult.Solution.Text}");
}
else
{
    Console.WriteLine($"Task failed: {taskResult.ErrorCode} - {taskResult.ErrorDescription}");
}
```

## API Client Methods

Here's an overview of the methods available on `IAnticaptchaClient`:

*   `Task<BalanceResponse> GetBalanceAsync(CancellationToken cancellationToken)`: [Docs](https://anti-captcha.com/apidoc/methods/getBalance)
    ```csharp
    var balanceResponse = await client.GetBalanceAsync();
    Console.WriteLine($"Current Balance: {balanceResponse.Balance}");
    ```
*   `Task<CreateTaskResponse> CreateCaptchaTaskAsync<T>(ICaptchaRequest<T> request, ...)`: [Docs](https://anti-captcha.com/apidoc/methods/createTask)
    ```csharp
    // See Two-Step Solving example above
    ```
*   `Task<TaskResultResponse<TSolution>> GetTaskResultAsync<TSolution>(int taskId, CancellationToken cancellationToken)`: [Docs](https://anti-captcha.com/apidoc/methods/getTaskResult)
    ```csharp
    // Checks task status once, might still be processing
    var result = await client.GetTaskResultAsync<ImageToTextSolution>(taskId);
    ```
*   `Task<TaskResultResponse<TSolution>> WaitForTaskResultAsync<TSolution>(int taskId, ...)`: (Uses `getTaskResult` internally with polling)
    ```csharp
    // See Two-Step Solving example above
    ```
*   `Task<TaskResultResponse<TSolution>> SolveCaptchaAsync<TSolution>(ICaptchaRequest<TSolution> request, ...)`: (Combines `CreateCaptchaTaskAsync` and `WaitForTaskResultAsync`)
    ```csharp
    // See Basic Usage example above
    ```
*   `Task<GetQueueStatsResponse> GetQueueStatsAsync(QueueType queueType, ...)`: [Docs](https://anti-captcha.com/apidoc/methods/getQueueStats)
    ```csharp
    var stats = await client.GetQueueStatsAsync(QueueType.RecaptchaV2Proxyless);
    ```
*   `Task<GetSpendingStatsResponse> GetSpendingStatsAsync(...)`: [Docs](https://anti-captcha.com/apidoc/methods/getSpendingStats)
    ```csharp
    var spending = await client.GetSpendingStatsAsync(queue: "ImageToTextTask");
    ```
*   `Task<GetAppStatsResponse> GetAppStatsAsync(int softId, ...)`: [Docs](https://anti-captcha.com/apidoc/methods/getAppStats)
    ```csharp
    // Replace 123 with your actual SoftID
    var appStats = await client.GetAppStatsAsync(123, AppStatsMode.Errors);
    ```
*   `Task<ActionResponse> ReportTaskOutcomeAsync(int taskId, ReportOutcome outcome, ...)`: [Docs for reporting](https://anti-captcha.com/apidoc/methods/reportIncorrectImageCaptcha) (Note: Method names vary in API docs, this library uses a single method)
    ```csharp
    // Example: Report an incorrectly solved Recaptcha
    if (taskResult.Status == TaskStatusType.Ready /* but solution was wrong */) {
        await client.ReportTaskOutcomeAsync(taskId, ReportOutcome.IncorrectRecaptcha);
    }
    ```
*   `Task<ActionResponse> PushAntiGateVariableAsync(int taskId, string name, object value, ...)`: [Docs](https://anti-captcha.com/apidoc/methods/pushAntiGateVariable) (For AntiGate custom tasks)
    ```csharp
    // await client.PushAntiGateVariableAsync(taskId, "varName", "varValue");
    ```

## Contributing

1.  Clone the repository: `git clone https://github.com/RemarkableSolutionsAdmin/AntiCaptchaApi.Net.git`
2.  Create a new branch for your feature or bug fix.
3.  Make your changes.
4.  Ensure tests pass (add new tests if applicable).
5.  Push your branch to your fork.
6.  Create a Pull Request.

## License

MIT - see the [LICENSE](LICENSE) file for details.

## Credits

Copyright (c) 2022-2025 Remarkable Solutions
