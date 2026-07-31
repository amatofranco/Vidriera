using MediatR;

namespace Vidriera.Application.Companies;

public record UpdateCompanyLogoCommand(Guid CompanyId, Stream FileContent, string ContentType) : IRequest;
