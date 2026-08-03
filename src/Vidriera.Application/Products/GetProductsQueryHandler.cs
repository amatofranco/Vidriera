using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Products;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    private readonly ISession _session;

    public GetProductsQueryHandler(ISession session)
    {
        _session = session;
    }

    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _session.Query<Product>()
            .Where(p => p.Company.Id == request.CompanyId && p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(cancellationToken);

        return products
            .Select(p => new ProductDto(p.Id, p.Name, p.Code, p.HasStock, !string.IsNullOrEmpty(p.SheetPdfBlobKey), p.Section?.Id, p.SortOrder))
            .ToList();
    }
}
