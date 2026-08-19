using MediatR;

namespace Vidriera.Application.Products;

public record CreateProductCommand(
    Guid CompanyId,
    Stream FileContent,
    string OriginalFileName,
    string? Name,
    string? Code) : IRequest<ProductDto>;
