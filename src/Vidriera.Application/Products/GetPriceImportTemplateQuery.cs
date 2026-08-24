using MediatR;

namespace Vidriera.Application.Products;

public record GetPriceImportTemplateQuery : IRequest<byte[]>;
