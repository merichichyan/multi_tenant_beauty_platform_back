namespace multi_tenant_beauty_platform_back.Domain.Exceptions;

public abstract class DomainException : Exception
{
    public int StatusCode { get; }

    protected DomainException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}
