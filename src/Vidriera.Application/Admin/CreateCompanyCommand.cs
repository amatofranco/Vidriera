using MediatR;

namespace Vidriera.Application.Admin;

public record CreateCompanyCommand(
    string CompanyName,
    string UserEmail,
    string UserName,
    string UserPassword,
    bool ShowCode = true,
    bool ShowPrice = true,
    bool ShowOrders = false,
    bool ShowCatalogLabel = true,
    string? Slug = null,
    string? Preset = null) : IRequest<CreateCompanyResult>;

public record CreateCompanyResult(Guid CompanyId, Guid UserId);
