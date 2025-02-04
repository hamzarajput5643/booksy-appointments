namespace API.Helpers
{
    public class BooksySettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string? Fingerprint { get; set; }
    }
}
