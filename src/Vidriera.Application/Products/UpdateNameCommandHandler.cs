using MediatR;
using NHibernate;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Products;

public class UpdateNameCommandHandler : IRequestHandler<UpdateNameCommand>
{
    private readonly ISession _session;

    public UpdateNameCommandHandler(ISession session)
    {
        _session = session;
    }

    public async Task Handle(UpdateNameCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrEmpty(name))
        {
            throw new ValidationException(ErrorMessages.ProductNameRequired);
        }

        var product = await _session.Query<Product>().GetOrThrowAsync(
            p => p.Id == request.ProductId && p.Company.Id == request.CompanyId,
            ErrorMessages.ProductNotFound(request.ProductId),
            cancellationToken);

        product.Name = name;

        await _session.UpdateInTransactionAsync(product, cancellationToken);
    }
}
