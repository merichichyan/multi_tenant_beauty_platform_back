using multi_tenant_beauty_platform_back.Domain.Entities;

namespace multi_tenant_beauty_platform_back.Domain.Services;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
