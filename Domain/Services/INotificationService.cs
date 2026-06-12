using System;
using System.Threading;
using System.Threading.Tasks;

namespace multi_tenant_beauty_platform_back.Domain.Services;

public interface INotificationService
{
    Task SendNotificationToUserAsync(Guid userId, string title, string message, CancellationToken ct = default);
}
