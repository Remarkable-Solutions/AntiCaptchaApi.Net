using System.Text.Json.Nodes;
using AntiCaptchaApi.Net;
using AntiCaptchaApi.Net.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

// Every ```csharp block of README.md, copied verbatim (the README's using directives are at the top of this file),
// so that the README examples are checked by the compiler. The methods are never run: they would call the paid API.
// ReadmeSnippetsTests (test project, UnitTests/Docs) fails when a README block is missing here or differs.
// Parameters stand in for values that the README assumes from the surrounding text (a client, a task, a task id).

internal static class ReadmeSnippets
{
    internal static async Task QuickStart()
    {
        var client = new AntiCaptchaClient("YOUR_API_KEY");

        var solution = await client.SolveAsync(
            new TurnstileTask("https://example.com/login", "0x4AAAAAAAA-your-site-key"));

        Console.WriteLine(solution.Token);
    }

    internal static async Task TrySolve(AntiCaptchaClient client)
    {
        var result = await client.TrySolveAsync(new TurnstileTask("https://example.com/login", "0x4AAAAAAAA-your-site-key"));

        if (result.IsSuccess)
        {
            Console.WriteLine($"{result.Solution!.Token} (task {result.TaskId}, {result.Cost} USD, {result.Attempts} attempt(s))");
        }
        else
        {
            Console.WriteLine($"Failed: {result.Error!.Code} - {result.Error.Description}");
        }
    }

    internal static async Task ImageToText(AntiCaptchaClient client)
    {
        var task = ImageToTextTask.FromFile("captcha.png") with
        {
            Numeric = NumericOption.NumbersOnly,
            MinLength = 4,
            MaxLength = 6,
        };
        var text = (await client.SolveAsync(task)).Text;
    }

    internal static async Task ImageToCoordinates(AntiCaptchaClient client)
    {
        var task = ImageToCoordinatesTask.FromFile("cats.jpeg") with
        {
            Comment = "Select all cats",
            Mode = ImageToCoordinatesMode.Rectangles,
        };
        foreach (var shape in (await client.SolveAsync(task)).Coordinates)
        {
            Console.WriteLine(string.Join(", ", shape)); // x1, y1, x2, y2 for a rectangle
        }
    }

    internal static async Task RecaptchaV2(AntiCaptchaClient client)
    {
        var solution = await client.SolveAsync(new RecaptchaV2Task("https://example.com/signup", "6Lc_your-site-key")
        {
            IsInvisible = true, // optional
        });
        Console.WriteLine(solution.GRecaptchaResponse);
    }

    internal static async Task RecaptchaV2Enterprise(AntiCaptchaClient client)
    {
        var solution = await client.SolveAsync(new RecaptchaV2EnterpriseTask("https://example.com/signup", "6Lc_your-site-key")
        {
            EnterprisePayload = new Dictionary<string, string> { ["s"] = "value-from-grecaptcha.enterprise.render" },
        });
        Console.WriteLine(solution.GRecaptchaResponse);
    }

    internal static async Task RecaptchaV3(AntiCaptchaClient client)
    {
        var solution = await client.SolveAsync(new RecaptchaV3Task("https://example.com/login", "6Lc_your-site-key")
        {
            MinScore = MinScore.Score07, // Score03 (default), Score07 or Score09
            PageAction = "login",
            IsEnterprise = false,        // true for reCAPTCHA v3 Enterprise
        });
        Console.WriteLine(solution.GRecaptchaResponse);
    }

    internal static async Task Turnstile(AntiCaptchaClient client)
    {
        var solution = await client.SolveAsync(new TurnstileTask("https://example.com/login", "0x4AAAAAAAA-your-site-key")
        {
            Action = "login", // optional; also CData and ChlPageData for challenge pages
        });
        Console.WriteLine(solution.Token);
    }

    internal static async Task FunCaptcha(AntiCaptchaClient client)
    {
        var solution = await client.SolveAsync(new FunCaptchaTask("https://example.com/signup", "DF9C4D87-CB7B-4062-9FEB-BADB6ADA61E6")
        {
            ApiJsSubdomain = "client-api.arkoselabs.com", // optional
        });
        Console.WriteLine(solution.Token);
    }

    internal static async Task GeeTestV3(AntiCaptchaClient client)
    {
        var solution = await client.SolveAsync(new GeeTestV3Task("https://example.com/login", "gt-from-the-page", "fresh-challenge"));
        Console.WriteLine($"{solution.Challenge} {solution.Validate} {solution.Seccode}");
    }

    internal static async Task GeeTestV4(AntiCaptchaClient client)
    {
        var solution = await client.SolveAsync(new GeeTestV4Task("https://example.com/login", "captcha-id-from-the-page")
        {
            InitParameters = new Dictionary<string, string> { ["riskType"] = "slide" }, // optional
        });
        Console.WriteLine($"{solution.CaptchaId} {solution.LotNumber} {solution.PassToken} {solution.GenTime} {solution.CaptchaOutput}");
    }

    internal static async Task AmazonWaf(AntiCaptchaClient client)
    {
        var solution = await client.SolveAsync(new AmazonWafTask(
            "https://example.com/protected",
            "AQIDAHjcYu/GjX+QlghicBgQ/7bFaQZ+m5FKCMDnO+vTbNg96AF5H1K", // "key" from the page
            "CgAHbCe2GgAAAAAj",                                        // "iv" from the page
            "9BUgmlm48F92WUoqv97a49ZuEJJ50TCk9MVr3C7WMtQ0X6flVbufM4n8m")); // "context" from the page
        Console.WriteLine(solution.Token); // the aws-waf-token cookie value
    }

    internal static async Task Prosopo(AntiCaptchaClient client)
    {
        var solution = await client.SolveAsync(new ProsopoTask("https://example.com/login", "your-prosopo-site-key"));
        Console.WriteLine(solution.Token);
    }

    internal static async Task FriendlyCaptcha(AntiCaptchaClient client)
    {
        var solution = await client.SolveAsync(new FriendlyCaptchaTask("https://example.com/login", "your-friendly-captcha-site-key"));
        Console.WriteLine(solution.Token);
    }

    internal static async Task AntiGate(AntiCaptchaClient client)
    {
        var solution = await client.SolveAsync(new AntiGateTask(
            "https://example.com/",
            "YOUR_TEMPLATE_NAME",
            new JsonObject { ["css_selector"] = "#content" })
        {
            DomainsOfInterest = new[] { "auth.example.com" }, // optional
        });
        Console.WriteLine(solution.Cookies?.ToJsonString());
    }

    internal static async Task AntiGatePushVariable(AntiCaptchaClient client, AntiGateTask antiGateTask)
    {
        var taskId = await client.CreateTaskAsync(antiGateTask);

        // Later, when the value is known:
        await client.PushAntiGateVariableAsync(taskId, "verification_code", "123456");

        var solution = await client.WaitForResultAsync<AntiGateSolution>(taskId);
    }

    internal static async Task Proxy(AntiCaptchaClient client)
    {
        var proxy = new Proxy(ProxyType.Http, "203.0.113.10", 3128)
        {
            Login = "user",
            Password = "secret",
        };

        var solution = await client.SolveAsync(new RecaptchaV2Task("https://example.com/signup", "6Lc_your-site-key")
        {
            Proxy = proxy,
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0 Safari/537.36",
        });
    }

    internal static async Task OtherApiMethods(AntiCaptchaClient client, int taskId)
    {
        decimal balance = await client.GetBalanceAsync();

        QueueStats queue = await client.GetQueueStatsAsync(QueueType.RecaptchaV2WithoutProxy);
        Console.WriteLine($"{queue.Waiting} workers waiting, load {queue.Load} %, bid {queue.Bid} USD");

        SpendingStats spending = await client.GetSpendingStatsAsync(new SpendingQuery { Date = DateTimeOffset.UtcNow.AddDays(-1) });
        foreach (var period in spending.Days)
        {
            Console.WriteLine($"{period.DateFrom:u}: {period.Volume} tasks, {period.Money} USD");
        }

        // The site rejected a token: tell anti-captcha.
        await client.ReportAsync(taskId, Report.IncorrectRecaptcha);
    }

    internal static async Task Configuration(AntiCaptchaClient client, TurnstileTask task)
    {
        var solution = await client.SolveAsync(task, new SolveOptions
        {
            Timeout = TimeSpan.FromMinutes(5),
            MaxAttempts = 2,
            LanguagePool = "rn",
        });
    }

    internal static void HttpClientFactory(IServiceCollection services)
    {
        services.AddHttpClient("anticaptcha");
        services.AddTransient<IAntiCaptchaClient>(sp => new AntiCaptchaClient(
            Environment.GetEnvironmentVariable("ANTICAPTCHA_API_KEY")!,
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("anticaptcha"),
            new AntiCaptchaClientOptions { RequestTimeout = TimeSpan.FromSeconds(20) }));
    }

    internal static async Task Errors(AntiCaptchaClient client, string url, string siteKey)
    {
        try
        {
            var solution = await client.SolveAsync(new TurnstileTask(url, siteKey));
        }
        catch (AntiCaptchaValidationException ex)
        {
            foreach (var error in ex.Errors)
            {
                Console.WriteLine($"{error.Field}: {error.Message}");
            }
        }
        catch (AntiCaptchaException ex) when (ex.Error.Code == AntiCaptchaErrorCode.ZeroBalance)
        {
            Console.WriteLine("Top up your anti-captcha balance.");
        }
    }

    internal static async Task Cancellation(AntiCaptchaClient client, TurnstileTask task)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        try
        {
            var solution = await client.SolveAsync(task, ct: cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Cancelled by us.");
        }
        catch (AntiCaptchaException ex) when (ex.Error.Code == AntiCaptchaErrorCode.SolveTimeout)
        {
            Console.WriteLine($"Task {ex.TaskId} was not ready in time.");
        }
    }

    internal static void OpenTelemetry()
    {
        using var tracing = Sdk.CreateTracerProviderBuilder()
            .AddSource(AntiCaptchaDiagnostics.ActivitySourceName)
            .AddConsoleExporter()
            .Build();

        using var metrics = Sdk.CreateMeterProviderBuilder()
            .AddMeter(AntiCaptchaDiagnostics.MeterName)
            .AddConsoleExporter()
            .Build();
    }
}
