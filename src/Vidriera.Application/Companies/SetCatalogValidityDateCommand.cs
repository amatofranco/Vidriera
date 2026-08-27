using MediatR;

namespace Vidriera.Application.Companies;

public record SetCatalogValidityDateCommand(Guid CompanyId, DateTime? CustomDate, bool Show) : IRequest;
