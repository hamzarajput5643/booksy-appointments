using Polly.Timeout;
using Polly;
using System.Net;
using System.Net.Http.Headers;
using API.Helpers;
using Polly.Extensions.Http;

namespace API.Configuration
{
    public static class HttpClientConfiguration
    {
        public static IServiceCollection AddSecureHttpClient(this IServiceCollection services, IConfiguration configuration)
        {
            // Validate configuration
            var booksySettings = configuration.GetSection("Booksy").Get<BooksySettings>();
            if (string.IsNullOrEmpty(booksySettings?.ApiKey) || string.IsNullOrEmpty(booksySettings?.AccessToken))
            {
                throw new InvalidOperationException("API keys are missing from configuration.");
            }

            services.AddHttpClient("SecureHttpClient", client =>
            {
                client.DefaultRequestVersion = HttpVersion.Version20;
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                client.BaseAddress = new Uri(booksySettings.BaseUrl);

                // Non-sensitive default headers
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip,
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    // Only allow valid certificates
                    return errors == System.Net.Security.SslPolicyErrors.None;
                }
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>()
                .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
        }
    }
}