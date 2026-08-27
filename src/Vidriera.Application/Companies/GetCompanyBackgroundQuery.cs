using MediatR;

namespace Vidriera.Application.Companies;

public record GetCompanyBackgroundQuery(Guid CompanyId) : IRequest<CompanyLogoResult>;
