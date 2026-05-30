using System;

namespace multi_tenant_beauty_platform_back.Domain.Entities;

public class Letter
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string UserEmail { get; private set; } = string.Empty;
    public string UserName { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    protected Letter() { }

    public Letter(Guid userId, string userEmail, string userName, string message)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        UserEmail = userEmail;
        UserName = userName;
        Message = message;
        CreatedAt = DateTime.UtcNow;
    }
}
