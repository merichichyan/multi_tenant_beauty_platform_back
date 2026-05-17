namespace multi_tenant_beauty_platform_back.Domain.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object key) 
        : base($"Entity '{entityName}' ({key}) was not found.", 404)
    {
    }
}
