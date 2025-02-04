﻿namespace API.Helpers
{
    public class AppSettings
    {
        public string Secret { get; set; }
        public string Audience { get; set; }
        public string Issuer { get; set; }
        public int RefreshTokenValidityInDays { get; set; }

    }
}