using MediatR;

namespace Vidriera.Application.Companies;

public record DeleteCompanyBackgroundCommand(Guid CompanyId) : IRequest;
