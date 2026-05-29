using multi_tenant_beauty_platform_back.Application.DTOs;
using multi_tenant_beauty_platform_back.Application.DTOs.Auth;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace multi_tenant_beauty_platform_back.Application.Services;

public class UserListItemDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public interface IUserService
{
    Task<PaginatedResponseDto<UserListItemDto>> GetUsersAsync(string? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task UpdateUserStatusAsync(Guid id, string status, CancellationToken cancellationToken = default);
}
