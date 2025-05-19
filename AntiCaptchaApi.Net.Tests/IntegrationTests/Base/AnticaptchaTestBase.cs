using AntiCaptchaApi.Net.Internal.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.IntegrationTests.Base
{
    [Collection("Sequential")]
    public class AnticaptchaTestBase
    {
        protected readonly IAnticaptchaClient AnticaptchaClient;
        protected readonly ServiceProvider ServiceProvider;

        public AnticaptchaTestBase()
        {
            var services = new ServiceCollection();
        
            services.AddAnticaptcha(TestEnvironment.ClientKey, config =>
            {
                // Configure test-specific options if needed, e.g., shorter timeouts
                // config.MaxWaitForTaskResultTimeMs = 30000; // 30 seconds for tests
                // config.DelayTimeBetweenCheckingTaskResultMs = 2000; // 2 seconds for tests
            });
            ServiceProvider = services.BuildServiceProvider();
            AnticaptchaClient =
                new AnticaptchaClient(TestEnvironment
                    .ClientKey); // ServiceProvider.GetRequiredService<IAnticaptchaClient>();
        }
    }
}