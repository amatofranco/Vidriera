using MediatR;

namespace Vidriera.Application.Items;

public record GetAvailabilityImportTemplateQuery : IRequest<byte[]>;
