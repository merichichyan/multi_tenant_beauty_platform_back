using multi_tenant_beauty_platform_back.Application.DTOs;
using multi_tenant_beauty_platform_back.Domain.Repositories;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace multi_tenant_beauty_platform_back.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<PaginatedResponseDto<UserListItemDto>> GetUsersAsync(string? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var (users, totalCount) = await _userRepository.GetAllUsersAsync(status, pageNumber, pageSize, cancellationToken);

        var userDtos = users.Select(u => new UserListItemDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            Role = u.Role,
            Status = u.Status,
            CreatedAt = u.CreatedAt
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PaginatedResponseDto<UserListItemDto>
        {
            Items = userDtos,
            Page = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            TotalCount = totalCount
        };
    }

    public async Task UpdateUserStatusAsync(Guid id, string status, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {id} not found.");
        }

        user.UpdateStatus(status);
        await _userRepository.UpdateAsync(user, cancellationToken);
    }
}
