using multi_tenant_beauty_platform_back.Application.DTOs;
using multi_tenant_beauty_platform_back.Domain.Entities;
using multi_tenant_beauty_platform_back.Domain.Repositories;

namespace multi_tenant_beauty_platform_back.Application.Services;

public class ServiceCategoryService : IServiceCategoryService
{
    private readonly IServiceCategoryRepository _repository;

    public ServiceCategoryService(IServiceCategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ServiceCategoryResponseDto>> GetAllAsync(CancellationToken ct = default)
    {
        var categories = await _repository.GetAllAsync(ct);
        return categories.Select(c => new ServiceCategoryResponseDto { Id = c.Id, Name = c.Name });
    }

    public async Task<ServiceCategoryResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var category = await _repository.GetByIdAsync(id, ct);
        if (category == null) return null;
        
        return new ServiceCategoryResponseDto { Id = category.Id, Name = category.Name };
    }

    public async Task<ServiceCategoryResponseDto> CreateAsync(ServiceCategoryRequestDto dto, CancellationToken ct = default)
    {
        var category = new ServiceCategory(dto.Name);
        await _repository.AddAsync(category, ct);
        return new ServiceCategoryResponseDto { Id = category.Id, Name = category.Name };
    }

    public async Task<ServiceCategoryResponseDto?> UpdateAsync(Guid id, ServiceCategoryRequestDto dto, CancellationToken ct = default)
    {
        var category = await _repository.GetByIdAsync(id, ct);
        if (category == null) return null;

        category.Update(dto.Name);
        await _repository.UpdateAsync(category, ct);
        
        return new ServiceCategoryResponseDto { Id = category.Id, Name = category.Name };
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var category = await _repository.GetByIdAsync(id, ct);
        if (category == null) return false;

        await _repository.DeleteAsync(category, ct);
        return true;
    }
}
