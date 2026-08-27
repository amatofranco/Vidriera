using MediatR;

namespace Vidriera.Application.Companies;

public record UpdateCompanyBackgroundCommand(Guid CompanyId, Stream FileContent, string ContentType) : IRequest;
