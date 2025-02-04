namespace API.Common
{
    public static class Utils
    {
        public static RequestResponse GetResponse(string message, object data = null, int statusCode = 400)
        {
            return new RequestResponse() { IsValid = false, Message = message, Data = data, StatusCode = statusCode, Errors = null };
        }
        public static RequestResponse GetErrorResponse(string message, object error = null, int statusCode = 400)
        {
            return new RequestResponse() { IsValid = false, Message = message, Data = null, StatusCode = statusCode, Errors = error };
        }
    }
}
