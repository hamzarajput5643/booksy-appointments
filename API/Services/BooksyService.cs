using API.Application.Models.Appointments;
using API.Application.Models.Businesses;
using API.Common;
using API.Helpers;
using Microsoft.Extensions.Options;

namespace API.Services
{
    public interface IBooksyService
    {
        Task<RequestResponse> GetAppointmentsAsync(
            int businessId, DateTime startDate, DateTime endDate, string? customerName = null);

        Task<RequestResponse> GetBusinessDataAsync();
    }

    public class BooksyService : IBooksyService
    {
        private readonly ITokenService _tokenService;
        private readonly IHttpRequestService _httpService;
        private readonly AppSettings _appSettings;

        public BooksyService(ITokenService tokenService, IHttpRequestService httpService, IOptions<AppSettings> appSettings)
        {
            _tokenService = tokenService;
            _httpService = httpService;
            _appSettings = appSettings.Value;
        }

        public async Task<RequestResponse> GetBusinessDataAsync()
        {
            var response = await _httpService.GetAsync<BusinessResponse>(ApiEndpoints.GetBusinessData);

            var business = response.Businesses.FirstOrDefault();
            if (business == null)
            {
                Utils.GetErrorResponse("No businesses found.", 404);
            }

            var refreshToken = _tokenService.GenerateRefreshToken();
            var tokenExpireDays = DateTime.UtcNow.AddDays(_appSettings.RefreshTokenValidityInDays);

            var accessToken = _tokenService.CreateToken(business.Id);

            var data = new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Expiration = DateTime.UtcNow.AddDays(_appSettings.RefreshTokenValidityInDays) 
            };

            return new RequestResponse { Data = data };
        }

        public async Task<RequestResponse> GetAppointmentsAsync(
            int businessId, DateTime startDate, DateTime endDate, string? customerName = null)
        {
            var requestUri = ApiEndpoints.GetAppointments(businessId, startDate, endDate, customerName);

            var response = await _httpService.GetAsync<object>(requestUri);

            if (response == null)
            {
                Utils.GetErrorResponse("No appointments found.", 404);
            }

            return new RequestResponse { Data = response ?? new() };
        }
    }
}