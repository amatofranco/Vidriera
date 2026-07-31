using MediatR;

namespace Vidriera.Application.Companies;

public record GetCompanyLogoQuery(Guid CompanyId) : IRequest<CompanyLogoResult>;

public record CompanyLogoResult(Stream Content, string ContentType);
