using MediatR;

namespace Vidriera.Application.Products;

public record UpdateProductSheetCommand(
    Guid CompanyId,
    Guid ProductId,
    Stream FileContent,
    string OriginalFileName) : IRequest;
