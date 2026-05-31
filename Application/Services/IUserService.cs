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

    public string? Phone { get; set; }
    public string? Gender { get; set; }
    public DateTime? Birthday { get; set; }
    public string? Address { get; set; }
    public string? Description { get; set; }
    public string? SocialMedias { get; set; }
    public string? LogoUrl { get; set; }
    public string? PreferredColors { get; set; }
    public string? WorkingHours { get; set; }
    public string? SalonName { get; set; }
    public string? OperatingHours { get; set; }
}

public interface IUserService
{
    Task<PaginatedResponseDto<UserListItemDto>> GetUsersAsync(string? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task UpdateUserStatusAsync(Guid id, string status, CancellationToken cancellationToken = default);
}
