using MediatR;

namespace Vidriera.Application.Companies;

public record UpdateCompanyCoverLogoCommand(Guid CompanyId, Stream FileContent, string ContentType) : IRequest;
