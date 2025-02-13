using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using VoipInnovations.Application.Exceptions;
using System.Xml;
using System.ComponentModel.DataAnnotations;
using System.ServiceModel;
using System.Diagnostics;
using System.Net;

namespace VoipInnovations.Application.Services
{
    public interface IApiService
    {
        Task<ApiResponse<DIDResponse>> CreateDIDGroupAsync(string name, CancellationToken cancellationToken = default);
        Task<ApiResponse<IntlResponse>> GlobalDIDSearchAsync(string areaCode, CancellationToken cancellationToken = default);
        Task<ApiResponse<DIDResponse>> ReserveDIDAsync(DIDParam[] didParams, CancellationToken cancellationToken = default);
        Task<ApiResponse<DIDResponse>> AssignDIDAsync(DIDParam[] didParams, CancellationToken cancellationToken = default);
        Task<ApiResponse<DIDResponse>> ConfigDIDAsync(DIDParam[] didParams, CancellationToken cancellationToken = default);
        Task<ApiResponse<SMSResponse>> ConfigSMSAsync(string tn, bool enable, string forwardEmail, CancellationToken cancellationToken = default);
        Task<ApiResponse<DIDResponse>> SetDIDGroupAsync(string tn, int groupId, CancellationToken cancellationToken = default);
        Task<ApiResponse<SMS10DLCResponse>> GetSMSCampaignsAsync(CancellationToken cancellationToken = default);
        Task<ApiResponse<SMS10DLCResponse>> AssignToSMSCampaignAsync(ArrayOfString tns, string campaignId, CancellationToken cancellationToken = default);
        Task<ApiResponse<DIDResponse>> RemoveDIDGroupAsync(string tn, CancellationToken cancellationToken = default);
        Task<ApiResponse<DIDResponse>> ReleaseDIDAsync(DIDParam[] didParams, CancellationToken cancellationToken = default);
    }
    public sealed record ApiResponse<T> where T : class
    {
        public required int ResponseCode { get; init; }
        public required string ResponseMessage { get; init; }
        public T? Result { get; init; }

        public bool IsSuccess => ResponseCode == 0;
        public bool IsFault => ResponseCode != 0;

        public static ApiResponse<T> Error(int code, string message) => new()
        {
            ResponseCode = code,
            ResponseMessage = message,
            Result = null
        };

        public static ApiResponse<T> Success(T result) => new()
        {
            ResponseCode = 0,
            ResponseMessage = "Success",
            Result = result
        };
    }

    public class VoipApiConfig : IValidatableObject
    {
        public required string Login { get; init; }
        public required string Secret { get; init; }
        public int RequestTimeoutSeconds { get; init; } = 30;
        public int MaxRetryAttempts { get; init; } = 3;
        public string DefaultCampaignId { get; init; } = "1";

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(Login))
            {
                yield return new ValidationResult("Login cannot be empty.", new[] { nameof(Login) });
            }

            if (string.IsNullOrEmpty(Secret))
            {
                yield return new ValidationResult("Secret cannot be empty.", new[] { nameof(Secret) });
            }

            if (RequestTimeoutSeconds <= 0)
            {
                yield return new ValidationResult("RequestTimeoutSeconds must be greater than 0.", new[] { nameof(RequestTimeoutSeconds) });
            }

            if (MaxRetryAttempts <= 0)
            {
                yield return new ValidationResult("MaxRetryAttempts must be greater than 0.", new[] { nameof(MaxRetryAttempts) });
            }
        }
    }

    public class SoapApiSettingsValidation : IValidateOptions<VoipApiConfig>
    {
        public ValidateOptionsResult Validate(string name, VoipApiConfig options)
        {
            if (string.IsNullOrEmpty(options.Login))
            {
                return ValidateOptionsResult.Fail("Option Information: Login cannot be empty.");
            }

            if (string.IsNullOrEmpty(options.Secret))
            {
                return ValidateOptionsResult.Fail("Option Information: Secret cannot be empty.");
            }

            return ValidateOptionsResult.Success;
        }
    }

    public class ApiService : IApiService
    {
        private readonly VoipApiConfig _settings;
        private readonly ILogger<ApiService> _logger;
        private readonly AsyncRetryPolicy _soapRetryPolicy;
        private readonly ActivitySource _activitySource;
        private bool _disposed;

        public ApiService(
            IOptions<VoipApiConfig> config,
            ILogger<ApiService> logger)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(logger);

            _settings = config.Value;
            _logger = logger;
            _activitySource = new ActivitySource("VoipInnovations.ApiService");
            _soapRetryPolicy = CreateRetryPolicy();

            var context = new ValidationContext(_settings);
            Validator.ValidateObject(_settings, context, validateAllProperties: true);
        }

        private AsyncRetryPolicy CreateRetryPolicy() =>
            Policy
            .Handle<Exception>(ex => ex is CommunicationException or TimeoutException)
            .WaitAndRetryAsync(
                _settings.MaxRetryAttempts,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                OnRetryAsync);

        private Task OnRetryAsync(Exception exception, TimeSpan timeSpan, int retryAttempt, Context context)
        {
            _logger.LogWarning(
                exception,
                "Retrying SOAP API call after {TimeSpan}ms due to {ExceptionType}: {Message} (Attempt {RetryAttempt})",
                timeSpan.TotalMilliseconds,
                exception.GetType().Name,
                exception.Message,
                retryAttempt);

            return Task.CompletedTask;
        }

        private APIServiceSoapClient CreateSoapClient()
        {
            try
            {
                if (string.IsNullOrEmpty(_settings.Login) || string.IsNullOrEmpty(_settings.Secret))
                {
                    string errorMessage = "SoapApiSettings: Login and Secret Must be configured";
                    _logger.LogError(errorMessage);
                    throw new SoapClientConfigurationException("SoapApiSettings: Login and Secret are required", new ArgumentException(errorMessage));
                }

                var client = new APIServiceSoapClient(APIServiceSoapClient.EndpointConfiguration.APIServiceSoap);

                client.ClientCredentials.UserName.UserName = _settings.Login;
                client.ClientCredentials.UserName.Password = _settings.Secret;
                client.Endpoint.Binding.SendTimeout = TimeSpan.FromSeconds(_settings.RequestTimeoutSeconds);

                return client;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring SOAP client");
                throw new SoapClientConfigurationException("Error configuring SOAP client", ex);
            }
        }

        private static void ValidatePhoneNumbers(params string?[] phoneNumbers)
        {
            foreach (var phoneNumber in phoneNumbers)
            {
                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    throw new ArgumentException("Phone number cannot be empty");
                }

                if (!phoneNumber.All(char.IsDigit))
                {
                    throw new ArgumentException($"Phone number must contain only digits: {phoneNumber}");
                }
            }
        }

        private async Task<ApiResponse<T>> ExecuteSoapCallAsync<T, TResult>(
           Func<APIServiceSoapClient, CancellationToken, Task<TResult>> soapCall,
           Func<TResult, T?> resultSelector,
           string? phoneNumberToValidate = null,
           CancellationToken cancellationToken = default) where T : class where TResult : class
        {
            using var activity = _activitySource.StartActivity("ExecuteSoapCall");
            activity?.SetTag("phoneNumber", phoneNumberToValidate);

            try
            {
                if (phoneNumberToValidate is not null)
                {
                    ValidatePhoneNumbers(phoneNumberToValidate);
                }

                return await _soapRetryPolicy.ExecuteAsync(async (ct) =>
                {
                    using (var cts = new CancellationTokenSource(_settings.RequestTimeoutSeconds * 1000))
                    {
                        using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token))
                        {
                            await using var client = CreateSoapClient();
                            var task = soapCall(client, linkedCts.Token);
                            var completedTask = await Task.WhenAny(task, Task.Delay(_settings.RequestTimeoutSeconds * 1000, ct));
                            if (completedTask == task)
                            {
                                try
                                {
                                    var soapResult = await task;

                                    if (soapResult is null)
                                    {
                                        _logger.LogError("SOAP API returned a null response");
                                        return ApiResponse<T>.Error(-5, "Unexpected null response from SOAP API");
                                    }

                                    var result = resultSelector(soapResult);
                                    if (result is null)
                                    {
                                        _logger.LogError("Failed to process SOAP response - result selector returned null");
                                        return ApiResponse<T>.Error(-1, "Failed to process SOAP response");
                                    }

                                    return ApiResponse<T>.Success(result);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Exception calling SOAP api.");
                                    return ApiResponse<T>.Error(-1, $"Unexpected error: {ex.Message}");
                                }
                            }
                            else
                            {
                                _logger.LogError("SOAP operation timed out");
                                client.Abort();
                                return ApiResponse<T>.Error(-2, "Request timed out");

                            }
                        }
                    }
                }, cancellationToken);
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "SOAP operation was cancelled by the caller");
                return ApiResponse<T>.Error(-7, "Operation cancelled by the caller");
            }
            catch (SoapClientConfigurationException ex)
            {
                _logger.LogError(ex, "SOAP client configuration error");
                return ApiResponse<T>.Error(-4, $"Configuration error: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument provided to SOAP call");
                return ApiResponse<T>.Error(-6, ex.Message);
            }
            catch (FaultException ex)
            {
                _logger.LogError(ex, "SOAP fault: {Code} - {Reason}", ex.Code, ex.Reason);
                var detail = await ExtractFaultDetailAsync(ex);
                return ApiResponse<T>.Error(-1, $"SOAP fault: {ex.Code} - {ex.Reason} - Detail: {detail}");
            }
            catch (CommunicationException ex)
            {
                _logger.LogError(ex, "SOAP communication error");
                return ApiResponse<T>.Error(-3, "Communication error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during SOAP call");
                return ApiResponse<T>.Error(-1, $"Unexpected error: {ex.Message}");
            }
        }

        private static async Task<string> ExtractFaultDetailAsync(FaultException faultException)
        {
            try
            {
                var fault = faultException.CreateMessageFault();
                if (!fault.HasDetail)
                {
                    return "No detail provided";
                }

                using var reader = fault.GetReaderAtDetailContents();
                var doc = new XmlDocument();
                await Task.Run(() => doc.Load(reader));
                return doc.OuterXml;
            }
            catch (Exception ex)
            {
                return $"Error accessing fault detail: {ex.Message}";
            }
        }

        // SOAP API Methods

        public async Task<ApiResponse<DIDResponse>> CreateDIDGroupAsync(
            string name,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return await ExecuteSoapCallAsync<DIDResponse, CreateDIDGroupResponse>(
                (client, ct) => client.CreateDIDGroupAsync(_settings.Login, _settings.Secret, name),
                response => response?.Body?.CreateDIDGroupResult,
                cancellationToken: cancellationToken);
        }

        public async Task<ApiResponse<IntlResponse>> GlobalDIDSearchAsync(
            string areaCode,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(areaCode);

            return await ExecuteSoapCallAsync<IntlResponse, GlobalDIDSearchResponse>(
                (client, ct) => client.GlobalDIDSearchAsync(
                    _settings.Login,
                    _settings.Secret,
                    string.Empty,
                    string.Empty,
                    areaCode,
                    false,
                    false),
                response => response?.Body?.GlobalDIDSearchResult,
                phoneNumberToValidate: null,
                cancellationToken);
        }

        public async Task<ApiResponse<DIDResponse>> ReserveDIDAsync(
            DIDParam[] didParams,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(didParams);
            if (!didParams.Any()) throw new ArgumentException("At least one DID parameter is required", nameof(didParams));

            ValidatePhoneNumbers(didParams.Select(p => p.tn).ToArray());

            return await ExecuteSoapCallAsync<DIDResponse, reserveDIDResponse>(
                (client, ct) => client.reserveDIDAsync(_settings.Login, _settings.Secret, didParams),
                response => response?.Body?.reserveDIDResult,
                phoneNumberToValidate: didParams.First().tn,
                cancellationToken);
        }

        public async Task<ApiResponse<DIDResponse>> AssignDIDAsync(
            DIDParam[] didParams,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(didParams);
            if (!didParams.Any()) throw new ArgumentException("At least one DID parameter is required", nameof(didParams));

            ValidatePhoneNumbers(didParams.Select(p => p.tn).ToArray());

            return await ExecuteSoapCallAsync<DIDResponse, assignDIDResponse>(
                (client, ct) => client.assignDIDAsync(_settings.Login, _settings.Secret, didParams),
                response => response?.Body?.assignDIDResult,
                phoneNumberToValidate: didParams.First().tn,
                cancellationToken);
        }

        public async Task<ApiResponse<DIDResponse>> ConfigDIDAsync(
            DIDParam[] didParams,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(didParams);
            if (!didParams.Any()) throw new ArgumentException("At least one DID parameter is required", nameof(didParams));

            ValidatePhoneNumbers(didParams.Select(p => p.tn).ToArray());

            return await ExecuteSoapCallAsync<DIDResponse, configDIDResponse>(
                (client, ct) => client.configDIDAsync(_settings.Login, _settings.Secret, didParams),
                response => response?.Body?.configDIDResult,
                phoneNumberToValidate: didParams.First().tn,
                cancellationToken);
        }

        public async Task<ApiResponse<SMSResponse>> ConfigSMSAsync(
         string tn,
         bool enable,
         string forwardEmail,
         CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(tn);
            ArgumentException.ThrowIfNullOrEmpty(forwardEmail);

            if (!Uri.TryCreate($"mailto:{forwardEmail}", UriKind.Absolute, out _))
            {
                throw new ArgumentException("Invalid email format", nameof(forwardEmail));
            }

            return await ExecuteSoapCallAsync<SMSResponse, ConfigSMSResponse>(
                (client, ct) => client.ConfigSMSAsync(_settings.Login, _settings.Secret, tn, enable, forwardEmail),
                response => response?.Body?.ConfigSMSResult,
                phoneNumberToValidate: tn,
                cancellationToken);
        }

        public async Task<ApiResponse<DIDResponse>> SetDIDGroupAsync(
            string tn,
            int groupId,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(tn);
            if (groupId <= 0) throw new ArgumentException("Group ID must be greater than 0", nameof(groupId));

            return await ExecuteSoapCallAsync<DIDResponse, SetDIDGroupResponse>(
                (client, ct) => client.SetDIDGroupAsync(_settings.Login, _settings.Secret, tn, groupId),
                response => response?.Body?.SetDIDGroupResult,
                phoneNumberToValidate: tn,
                cancellationToken);
        }

        public async Task<ApiResponse<SMS10DLCResponse>> GetSMSCampaignsAsync(
            CancellationToken cancellationToken = default)
        {
            return await ExecuteSoapCallAsync<SMS10DLCResponse, GetSMSCampaignsResponse>(
                (client, ct) => client.GetSMSCampaignsAsync(_settings.Login, _settings.Secret),
                response => response?.Body?.GetSMSCampaignsResult,
                cancellationToken: cancellationToken);
        }

        public async Task<ApiResponse<SMS10DLCResponse>> AssignToSMSCampaignAsync(
            ArrayOfString tns,
            string campaignId,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(tns);
            ArgumentException.ThrowIfNullOrEmpty(campaignId);

            if (!tns.Any()) throw new ArgumentException("At least one phone number is required", nameof(tns));
            ValidatePhoneNumbers(tns.ToArray());

            return await ExecuteSoapCallAsync<SMS10DLCResponse, AssignToSMSCampaignResponse>(
                (client, ct) => client.AssignToSMSCampaignAsync(_settings.Login, _settings.Secret, tns, campaignId),
                response => response?.Body?.AssignToSMSCampaignResult,
                phoneNumberToValidate: tns.FirstOrDefault(),
                cancellationToken);
        }

        public async Task<ApiResponse<DIDResponse>> RemoveDIDGroupAsync(
            string tn,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(tn);

            return await ExecuteSoapCallAsync<DIDResponse, RemoveDIDGroupResponse>(
                (client, ct) => client.RemoveDIDGroupAsync(_settings.Login, _settings.Secret, tn),
                response => response?.Body?.RemoveDIDGroupResult,
                phoneNumberToValidate: tn,
                cancellationToken);
        }

        public async Task<ApiResponse<DIDResponse>> ReleaseDIDAsync(
            DIDParam[] didParams,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(didParams);
            if (!didParams.Any()) throw new ArgumentException("At least one DID parameter is required", nameof(didParams));

            ValidatePhoneNumbers(didParams.Select(p => p.tn).ToArray());

            return await ExecuteSoapCallAsync<DIDResponse, releaseDIDResponse>(
                (client, ct) => client.releaseDIDAsync(_settings.Login, _settings.Secret, didParams),
                response => response?.Body?.releaseDIDResult,
                phoneNumberToValidate: didParams.First().tn,
                cancellationToken);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _activitySource.Dispose();
                }

                _disposed = true;
            }
        }
    }
}