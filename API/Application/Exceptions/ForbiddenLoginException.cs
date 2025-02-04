namespace API.Application.Exceptions;

public class ForbiddenLoginException : Exception
{
    public ForbiddenLoginException(string message)
        : base(message)
    {
        
    }
}
