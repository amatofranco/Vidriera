using MediatR;

namespace Vidriera.Application.Companies;

public record DeleteCompanyCoverLogoCommand(Guid CompanyId) : IRequest;
