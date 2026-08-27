using MediatR;

namespace Vidriera.Application.Companies;

public record GetCompanyCoverLogoQuery(Guid CompanyId) : IRequest<CompanyLogoResult>;
