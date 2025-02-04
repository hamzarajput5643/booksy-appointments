using System.Security.Claims;

namespace API.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetBusinessId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst("businessId")
                ?? throw new UnauthorizedAccessException("Business ID claim is missing");

            if (!int.TryParse(claim.Value, out int businessId))
            {
                throw new InvalidOperationException("Invalid business ID format");
            }

            return businessId;
        }
    }
}