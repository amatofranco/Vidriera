namespace Vidriera.Application.Abstractions;

public interface IJwtTokenGenerator
{
    string GenerateToken(Guid userId, Guid companyId, string email);
}
