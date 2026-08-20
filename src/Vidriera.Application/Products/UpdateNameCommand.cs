using MediatR;

namespace Vidriera.Application.Products;

public record UpdateNameCommand(Guid CompanyId, Guid ProductId, string Name) : IRequest;
