using MediatR;
using NHibernate;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Products;

public class UpdatePriceCommandHandler : IRequestHandler<UpdatePriceCommand>
{
    private readonly ISession _session;

    public UpdatePriceCommandHandler(ISession session)
    {
        _session = session;
    }

    public async Task Handle(UpdatePriceCommand request, CancellationToken cancellationToken)
    {
        var product = await _session.Query<Product>().GetOrThrowAsync(
            p => p.Id == request.ProductId && p.Company.Id == request.CompanyId,
            ErrorMessages.ProductNotFound(request.ProductId),
            cancellationToken);

        product.Price = request.Price;

        await _session.UpdateInTransactionAsync(product, cancellationToken);
    }
}
