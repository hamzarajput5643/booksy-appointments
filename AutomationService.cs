using Microsoft.Extensions.Options;
using VoipInnovations.Application.Services;

namespace VoipInnovations.Core.Services
{
    public interface IApiAutomationService
    {
        Task<(bool success, string message)> ActivateDID(string areaCode, string didGroupName, string webhookUrl);
        Task<(bool success, string message)> DeactivateDID(string didNumber);
    }

    public async Task<(bool success, string message)> ActivateDID(string areaCode, string organizationId, string webhookUrl, string mobileNumber)
        {
            try
            {
                // 1. Get DID Group Name from Organization

                var organization = await _organizationService.GetOrganizationAsync(organizationId);

                if (organization is null)
                {
                    _logger.LogError("Organization not found for organization: {Message}", organizationId);
                    return (false, $"Organization not found for organization: {organizationId}");
                }

                string didGroupName = organization.DidGroupName;
                if (string.IsNullOrEmpty(didGroupName))
                {
                    _logger.LogError("Organization: {organizationId} does not have DID Group Name value", organizationId);
                    return (false, $"Organization: {organizationId} does not have DID Group Name value");

                }
                // 2. Create DID Group
                var createGroupResponse = await _apiService.CreateDIDGroupAsync(didGroupName);
                if (createGroupResponse.IsFault)
                {
                    _logger.LogError("Failed to create DID group: {Message}", createGroupResponse.ResponseMessage);
                    return (false, $"Failed to create DID group: {createGroupResponse.ResponseMessage}");
                }

                // Ensure createGroupResponse.Result is not null before accessing ID
                if (createGroupResponse.Result?.DIDGroups == null || !createGroupResponse.Result.DIDGroups.Any())
                {
                    _logger.LogError("DID Group creation did not return DID Group ID.");
                    return (false, "DID Group creation did not return a DID Group ID");
                }

                int groupId = createGroupResponse.Result.DIDGroups.First().ID;

                // 3. Search for DID
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

                // 4. Reserve DID
                var reserveResponse = await _apiService.ReserveDIDAsync(didParams);
                if (reserveResponse.IsFault)
                {
                    _logger.LogError("Failed to reserve DID: {Message}", reserveResponse.ResponseMessage);
                    return (false, $"Failed to reserve DID: {reserveResponse.ResponseMessage}");
                }

                // 5. Assign DID to DID Group
                var assignResponse = await _apiService.AssignDIDAsync(didParams);
                if (assignResponse.IsFault)
                {
                    _logger.LogError("Failed to assign DID to group: {Message}", assignResponse.ResponseMessage);
                    return (false, $"Failed to assign DID to group: {assignResponse.ResponseMessage}");
                }

                // 6. Set DID Group

                var setGroupResponse = await _apiService.SetDIDGroupAsync(mobileNumber, groupId);
                if (setGroupResponse.IsFault)
                {
                    _logger.LogError("Failed to set DID group: {Message}", setGroupResponse.ResponseMessage);
                    return (false, $"Failed to set DID group: {setGroupResponse.ResponseMessage}");
                }

                // 7. Config DID
                var configResponse = await _apiService.ConfigDIDAsync(didParams);
                if (configResponse.IsFault)
                {
                    _logger.LogError("Failed to config DID: {Message}", configResponse.ResponseMessage);
                    return (false, $"Failed to config DID: {configResponse.ResponseMessage}");
                }

                // 8. Get SMS Campaigns
                var getCampaignsResponse = await _apiService.GetSMSCampaignsAsync();
                if (getCampaignsResponse.IsFault)
                {
                    _logger.LogError("Failed to get SMS Campaigns: {Message}", getCampaignsResponse.ResponseMessage);
                    return (false, $"Failed to get SMS Campaigns: {getCampaignsResponse.ResponseMessage}");
                }

                if (getCampaignsResponse.Result?.Campaigns == null || !getCampaignsResponse.Result.Campaigns.Any())
                {
                    _logger.LogWarning("No SMS Campaigns found.");
                    return (false, "No SMS Campaigns found.");
                }

                string campaignId = getCampaignsResponse.Result.Campaigns.First().CampaignId;

                // 9. Assign to SMS Campaign
                var tns = new ArrayOfString { mobileNumber };

                var assignCampaignResponse = await _apiService.AssignToSMSCampaignAsync(tns, campaignId);
                if (assignCampaignResponse.IsFault)
                {
                    _logger.LogError("Failed to assign DID to SMS campaign: {Message}", assignCampaignResponse.ResponseMessage);
                    return (false, $"Failed to assign DID to SMS campaign: {assignCampaignResponse.ResponseMessage}");
                }

                _logger.LogInformation("DID Activation successful for TN {TN}", mobileNumber);
                return (true, $"DID Activation successful for TN {mobileNumber}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during DID activation");
                return (false, $"Exception during DID activation: {ex.Message}");
            }
        }
    }