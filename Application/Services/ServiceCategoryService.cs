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

    public async Task<IEnumerable<ServiceCategoryResponseDto>> GetAllAsync(string? lang = null, CancellationToken ct = default)
    {
        var categories = await _repository.GetAllAsync(ct);
        return categories.Select(c => MapToResponse(c, lang));
    }

    public async Task<ServiceCategoryResponseDto?> GetByIdAsync(Guid id, string? lang = null, CancellationToken ct = default)
    {
        var category = await _repository.GetByIdAsync(id, ct);
        if (category == null) return null;
        
        return MapToResponse(category, lang);
    }

    public async Task<ServiceCategoryResponseDto> CreateAsync(ServiceCategoryRequestDto dto, CancellationToken ct = default)
    {
        var nameHy = !string.IsNullOrWhiteSpace(dto.NameHy) ? dto.NameHy : (dto.Name ?? string.Empty);
        var nameRu = !string.IsNullOrWhiteSpace(dto.NameRu) ? dto.NameRu : (dto.Name ?? string.Empty);
        var nameEn = !string.IsNullOrWhiteSpace(dto.NameEn) ? dto.NameEn : (dto.Name ?? string.Empty);

        var category = new ServiceCategory(nameHy, nameRu, nameEn);
        await _repository.AddAsync(category, ct);
        return MapToResponse(category, null);
    }

    public async Task<ServiceCategoryResponseDto?> UpdateAsync(Guid id, ServiceCategoryRequestDto dto, CancellationToken ct = default)
    {
        var category = await _repository.GetByIdAsync(id, ct);
        if (category == null) return null;

        var nameHy = !string.IsNullOrWhiteSpace(dto.NameHy) ? dto.NameHy : (dto.Name ?? string.Empty);
        var nameRu = !string.IsNullOrWhiteSpace(dto.NameRu) ? dto.NameRu : (dto.Name ?? string.Empty);
        var nameEn = !string.IsNullOrWhiteSpace(dto.NameEn) ? dto.NameEn : (dto.Name ?? string.Empty);

        category.Update(nameHy, nameRu, nameEn);
        await _repository.UpdateAsync(category, ct);
        
        return MapToResponse(category, null);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var category = await _repository.GetByIdAsync(id, ct);
        if (category == null) return false;

        await _repository.DeleteAsync(category, ct);
        return true;
    }

    private static ServiceCategoryResponseDto MapToResponse(ServiceCategory category, string? lang)
    {
        var name = lang?.ToLower() switch
        {
            "hy" => category.NameHy,
            "ru" => category.NameRu,
            "en" => category.NameEn,
            _ => category.NameEn // Default fallback
        };

        if (string.IsNullOrEmpty(name))
        {
            name = !string.IsNullOrEmpty(category.NameEn) ? category.NameEn :
                   (!string.IsNullOrEmpty(category.NameHy) ? category.NameHy : category.NameRu);
        }

        return new ServiceCategoryResponseDto
        {
            Id = category.Id,
            Name = name,
            NameHy = category.NameHy,
            NameRu = category.NameRu,
            NameEn = category.NameEn
        };
    }
}
