using MediatR;

namespace Vidriera.Application.Admin;

public record GetCompaniesQuery : IRequest<IReadOnlyList<CompanyListItemDto>>;

public record CompanyListItemDto(Guid Id, string Name, string? Slug, bool IsActive, DateTime CreatedAt);
