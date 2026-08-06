using MediatR;

namespace Vidriera.Application.Catalogs;

public record GetCompanyCatalogVersionQuery(Guid CompanyId) : IRequest<Guid?>;
