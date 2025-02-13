using Microsoft.Extensions.Options;
using VoipInnovations.Application.Services;

namespace VoipInnovations.Core.Services
{
    public interface IApiAutomationService
    {
        Task<(bool success, string message)> ActivateDID(string areaCode, string didGroupName, string webhookUrl, string didGroupId);
        Task<(bool success, string message)> DeactivateDID(string didNumber);
    }

    public class ApiAutomationService : IApiAutomationService
    {
        private readonly IApiService _apiService;
        private readonly ILogger<ApiAutomationService> _logger;
        private readonly VoipApiConfig _voipApiConfig;

        public ApiAutomationService(
            IApiService apiService,
            ILogger<ApiAutomationService> logger,
            IOptions<VoipApiConfig> voipApiConfig)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _voipApiConfig = voipApiConfig?.Value ?? throw new ArgumentNullException(nameof(voipApiConfig));
        }

        public async Task<(bool success, string message)> ActivateDID(string areaCode, string didGroupName, string webhookUrl, string didGroupId)
        {
            try
            {
                // 1. Create DID Group
                var createGroupResponse = await _apiService.CreateDIDGroupAsync(didGroupName);
                if (createGroupResponse.IsFault)
                {
                    _logger.LogError("Failed to create DID group: {Message}", createGroupResponse.ResponseMessage);
                    return (false, $"Failed to create DID group: {createGroupResponse.ResponseMessage}");
                }

                // 2. Search for DID
                var searchResponse = await _apiService.GlobalDIDSearchAsync(areaCode);
                if (searchResponse.IsFault)
                {
                    _logger.LogError("DID Search failed: {Message}", searchResponse.ResponseMessage);
                    return (false, $"DID Search failed: {searchResponse.ResponseMessage}");
                }

                if (searchResponse.Result?.DIDs == null || !searchResponse.Result.DIDs.Any())
                {
                    _logger.LogWarning("No DIDs found for area code {AreaCode}", areaCode);
                    return (false, $"No DIDs found for area code {areaCode}");
                }

                var didParams = new[] { new DIDParam { tn = searchResponse.Result.DIDs.First().tn } };

                // 3. Reserve DID
                var reserveResponse = await _apiService.ReserveDIDAsync(didParams);
                if (reserveResponse.IsFault)
                {
                    _logger.LogError("Failed to reserve DID: {Message}", reserveResponse.ResponseMessage);
                    return (false, $"Failed to reserve DID: {reserveResponse.ResponseMessage}");
                }

                // 4. Assign DID to DID Group
                var assignResponse = await _apiService.AssignDIDAsync(didParams);
                if (assignResponse.IsFault)
                {
                    _logger.LogError("Failed to assign DID to group: {Message}", assignResponse.ResponseMessage);
                    return (false, $"Failed to assign DID to group: {assignResponse.ResponseMessage}");
                }

                // 5. Set DID Group
                if (!int.TryParse(didGroupId, out var groupId))
                {
                    _logger.LogError("Invalid DID Group ID: {DidGroupId}", didGroupId);
                    return (false, "Invalid DID Group ID format");
                }

                var setGroupResponse = await _apiService.SetDIDGroupAsync(searchResponse.Result.DIDs.First().tn, groupId);
                if (setGroupResponse.IsFault)
                {
                    _logger.LogError("Failed to set DID group: {Message}", setGroupResponse.ResponseMessage);
                    return (false, $"Failed to set DID group: {setGroupResponse.ResponseMessage}");
                }

                // 6. Config DID
                var configResponse = await _apiService.ConfigDIDAsync(didParams);
                if (configResponse.IsFault)
                {
                    _logger.LogError("Failed to config DID: {Message}", configResponse.ResponseMessage);
                    return (false, $"Failed to config DID: {configResponse.ResponseMessage}");
                }

                // 7. Assign to SMS Campaign
                var tns = new ArrayOfString { searchResponse.Result.DIDs.First().tn };
                // Access the DefaultCampaignId from the configuration
                string campaignId = _voipApiConfig.DefaultCampaignId;

                var assignCampaignResponse = await _apiService.AssignToSMSCampaignAsync(tns, campaignId);
                if (assignCampaignResponse.IsFault)
                {
                    _logger.LogError("Failed to assign DID to SMS campaign: {Message}", assignCampaignResponse.ResponseMessage);
                    return (false, $"Failed to assign DID to SMS campaign: {assignCampaignResponse.ResponseMessage}");
                }

                _logger.LogInformation("DID Activation successful for TN {TN}", searchResponse.Result.DIDs.First().tn);
                return (true, $"DID Activation successful for TN {searchResponse.Result.DIDs.First().tn}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during DID activation");
                return (false, $"Exception during DID activation: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> DeactivateDID(string didNumber)
        {
            try
            {
                // 1. Remove DID Group
                var removeGroupResponse = await _apiService.RemoveDIDGroupAsync(didNumber);
                if (removeGroupResponse.IsFault)
                {
                    _logger.LogError("Failed to remove DID from group: {Message}", removeGroupResponse.ResponseMessage);
                    return (false, $"Failed to remove DID from group: {removeGroupResponse.ResponseMessage}");
                }

                // 2. Release DID
                var didParams = new[] { new DIDParam { tn = didNumber } };
                var releaseResponse = await _apiService.ReleaseDIDAsync(didParams);
                if (releaseResponse.IsFault)
                {
                    _logger.LogError("Failed to release DID: {Message}", releaseResponse.ResponseMessage);
                    return (false, $"Failed to release DID: {releaseResponse.ResponseMessage}");
                }

                _logger.LogInformation("DID Deactivation successful for TN {TN}", didNumber);
                return (true, $"DID Deactivation successful for TN {didNumber}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during DID deactivation");
                return (false, $"Exception during DID deactivation: {ex.Message}");
            }
        }
    }
}