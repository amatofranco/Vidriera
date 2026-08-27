using MediatR;

namespace Vidriera.Application.Companies;

public record SetCatalogSubtitleCommand(Guid CompanyId, string? Subtitle) : IRequest;
