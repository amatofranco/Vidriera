using MediatR;

namespace Vidriera.Application.Companies;

public record GetCatalogCoverSettingsQuery(Guid CompanyId) : IRequest<CatalogCoverSettingsDto>;

public record CatalogCoverSettingsDto(
    bool HasCoverLogo,
    string? CatalogSubtitle,
    bool HasCustomBackground,
    DateTime? CustomValidityDate,
    bool ShowValidityDate,
    DateTime? LastCatalogGeneratedAt);
