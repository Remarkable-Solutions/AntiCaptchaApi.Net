using System;
using System.Net.Http;
using AntiCaptchaApi.Net.Internal.Helpers; // Added this line
using AntiCaptchaApi.Net.Internal.Services;
using AntiCaptchaApi.Net.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AntiCaptchaApi.Net
{
    /// <summary>
    /// Extension methods for setting up AntiCaptcha services in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class AnticaptchaServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="IAnticaptchaClient"/> and related services to the specified <see cref="IServiceCollection"/>.
        /// This method configures the necessary HttpClient instances using <see cref="IHttpClientFactory"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="clientKey">Your Anti-Captcha API client key.</param>
        /// <param name="configureClient">An optional action to configure the <see cref="ClientConfig"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> or <paramref name="clientKey"/> is null.</exception>
        public static IServiceCollection AddAnticaptcha(this IServiceCollection services, string clientKey, Action<ClientConfig> configureClient = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrEmpty(clientKey))
            {
                throw new ArgumentNullException(nameof(clientKey));
            }

            // Ensure HttpClientFactory is available
            services.AddHttpClient();

            // Configure ClientConfig
            var clientConfig = new ClientConfig();
            configureClient?.Invoke(clientConfig);
            services.TryAddSingleton(clientConfig); // Register ClientConfig as singleton

            // Register HttpHelper
            services.TryAddScoped<IHttpHelper, HttpHelper>();
            
            // Register internal API service
            // It depends on IHttpHelper (which in turn depends on IHttpClientFactory)
            services.TryAddScoped<IAnticaptchaApi, AnticaptchaApi>();

            // Register the main client service
            // Use the internal constructor which accepts the dependencies
            services.TryAddScoped<IAnticaptchaClient>(provider =>
            {
                var api = provider.GetRequiredService<IAnticaptchaApi>();
                var config = provider.GetRequiredService<ClientConfig>();
                // Pass the clientKey provided during registration
                return new AnticaptchaClient(api, clientKey, config); 
            });

            return services;
        }
    }
}