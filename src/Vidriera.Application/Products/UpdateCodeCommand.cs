using MediatR;

namespace Vidriera.Application.Products;

public record UpdateCodeCommand(Guid CompanyId, Guid ProductId, string? Code) : IRequest;
