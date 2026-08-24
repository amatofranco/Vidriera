using MediatR;
using NHibernate;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Products;

public class UpdateCodeCommandHandler : IRequestHandler<UpdateCodeCommand>
{
    private readonly ISession _session;

    public UpdateCodeCommandHandler(ISession session)
    {
        _session = session;
    }

    public async Task Handle(UpdateCodeCommand request, CancellationToken cancellationToken)
    {
        var product = await _session.Query<Product>().GetOrThrowAsync(
            p => p.Id == request.ProductId && p.Company.Id == request.CompanyId,
            ErrorMessages.ProductNotFound(request.ProductId),
            cancellationToken);

        product.Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();

        await _session.UpdateInTransactionAsync(product, cancellationToken);
    }
}
