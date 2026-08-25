using MediatR;
using Vidriera.Application.Abstractions;

namespace Vidriera.Application.Items;

public class GetPriceImportTemplateQueryHandler : IRequestHandler<GetPriceImportTemplateQuery, byte[]>
{
    private readonly IPriceImportService _priceImportService;

    public GetPriceImportTemplateQueryHandler(IPriceImportService priceImportService)
    {
        _priceImportService = priceImportService;
    }

    public Task<byte[]> Handle(GetPriceImportTemplateQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(_priceImportService.GenerateTemplate());
}
