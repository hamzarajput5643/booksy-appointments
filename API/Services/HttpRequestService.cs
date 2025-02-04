using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Polly;
using Polly.Timeout;
using API.Helpers;

namespace API.Services
{
    public interface IHttpRequestService
    {
        Task<TResponse> GetAsync<TResponse>(string url);
        Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest requestBody);
        Task<TResponse> PutAsync<TRequest, TResponse>(string url, TRequest requestBody);
        Task<TResponse> DeleteAsync<TResponse>(string url);
    }

    public class HttpRequestService : IHttpRequestService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HttpRequestService> _logger;
        private readonly IConfiguration _configuration;
        private readonly JsonSerializerOptions _serializerOptions;
        private readonly IAsyncPolicy<HttpResponseMessage> _resiliencePolicy;

        public HttpRequestService(
            IHttpClientFactory httpClientFactory,
            ILogger<HttpRequestService> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient("SecureHttpClient");
            _logger = logger;
            _configuration = configuration;

            _serializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false,
            };

            // Resilience policy: Retry and timeout handling
            _resiliencePolicy = Policy<HttpResponseMessage>
               .Handle<HttpRequestException>()
               .OrResult(r => !r.IsSuccessStatusCode)
               .Or<TimeoutRejectedException>()
               .WaitAndRetryAsync(3, retryAttempt =>
                   TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                   onRetry: (outcome, timespan, retryAttempt, context) =>
                   {
                       _logger.LogWarning($"Retry {retryAttempt} due to failure: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
                   });
        }
        public async Task<TResponse> GetAsync<TResponse>(string url) =>
            await ExecuteRequest<TResponse>(HttpMethod.Get, url);

        public async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest requestBody) =>
            await ExecuteRequest<TResponse>(HttpMethod.Post, url, requestBody);

        public async Task<TResponse> PutAsync<TRequest, TResponse>(string url, TRequest requestBody) =>
            await ExecuteRequest<TResponse>(HttpMethod.Put, url, requestBody);

        public async Task<TResponse> DeleteAsync<TResponse>(string url) =>
            await ExecuteRequest<TResponse>(HttpMethod.Delete, url);

        #region private methods

        private async Task<TResponse> ExecuteRequest<TResponse>(HttpMethod method, string url, object? requestBody = null)
        {
            try
            {
                Func<HttpRequestMessage> requestFactory = () =>
                {
                    var request = new HttpRequestMessage(method, url)
                    {
                        Content = requestBody == null ? null : CreateJsonContent(requestBody)
                    };

                    AddSecurityHeaders(request);
                    return request;
                };

                var response = await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var request = requestFactory();
                    var result = await _httpClient.SendAsync(request);
                    result.EnsureSuccessStatusCode();
                    return result;
                });

                return await HandleResponse<TResponse>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for URL: {Url}", url);
                throw new HttpRequestException("Request failed after retries", ex);
            }
        }

        private void AddSecurityHeaders(HttpRequestMessage request)
        {
            var booksySettings = _configuration.GetSection("Booksy").Get<BooksySettings>();

            // Ensure security-sensitive headers are added if available
            if (!string.IsNullOrEmpty(booksySettings?.ApiKey))
            {
                request.Headers.Add("x-api-key", booksySettings.ApiKey);
            }

            if (!string.IsNullOrEmpty(booksySettings?.AccessToken))
            {
                request.Headers.Add("x-access-token", booksySettings.AccessToken);
            }

            // Non-sensitive headers
            request.Headers.Add("User-Agent", "SecureApiClient/1.0");
            request.Headers.Add("X-Request-ID", Guid.NewGuid().ToString());
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private HttpContent CreateJsonContent(object data)
        {
            var json = JsonSerializer.Serialize(data);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private async Task<TResponse> HandleResponse<TResponse>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();

            try
            {
                return JsonSerializer.Deserialize<TResponse>(content, _serializerOptions)
                       ?? throw new InvalidOperationException("Deserialization returned null.");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize response content.");
                throw new InvalidOperationException("Invalid response format", ex);
            }
        }

        #endregion
    }
}