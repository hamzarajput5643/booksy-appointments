namespace API.Helpers
{
    public static class ApiEndpoints
    {
        // Endpoint to get business data
        public const string GetBusinessData = "business_api/me/businesses/?businesses_page=1&businesses_per_page=1";

        // Endpoint to get appointments for a specific business
        public static string GetAppointments(int businessId, DateTime startDate, DateTime endDate, string? customerName)
        {
            return $"business_api/me/businesses/{businessId}/calendar?" +
                   $"start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}&" +
                   $"customer_name={customerName}&include_unconfirmed=true";
        }
    }
}