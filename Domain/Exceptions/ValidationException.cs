namespace multi_tenant_beauty_platform_back.Domain.Exceptions;

public class ValidationException : DomainException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(string message) : base(message, 422)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> errors) 
        : base("One or more validation failures have occurred.", 422)
    {
        Errors = errors;
    }
}
