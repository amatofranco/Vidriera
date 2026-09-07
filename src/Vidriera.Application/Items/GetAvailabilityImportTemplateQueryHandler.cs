using MediatR;
using Vidriera.Application.Abstractions;

namespace Vidriera.Application.Items;

public class GetAvailabilityImportTemplateQueryHandler : IRequestHandler<GetAvailabilityImportTemplateQuery, byte[]>
{
    private readonly IAvailabilityImportService _availabilityImportService;

    public GetAvailabilityImportTemplateQueryHandler(IAvailabilityImportService availabilityImportService)
    {
        _availabilityImportService = availabilityImportService;
    }

    public Task<byte[]> Handle(GetAvailabilityImportTemplateQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(_availabilityImportService.GenerateTemplate());
}
