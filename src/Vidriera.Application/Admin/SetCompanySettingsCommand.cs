using MediatR;

namespace Vidriera.Application.Admin;

public record SetCompanySettingsCommand(Guid CompanyId, bool ShowCode, bool ShowPrice, bool ShowOrders, string? CatalogSubtitle, string? Slug, string? CustomDomain) : IRequest;
