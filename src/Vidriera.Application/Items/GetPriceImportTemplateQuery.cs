using MediatR;

namespace Vidriera.Application.Items;

public record GetPriceImportTemplateQuery : IRequest<byte[]>;
