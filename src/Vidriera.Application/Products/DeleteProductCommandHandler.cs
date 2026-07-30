using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Products;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand>
{
    private readonly ISession _session;
    private readonly IBlobStorageService _blobStorageService;

    public DeleteProductCommandHandler(ISession session, IBlobStorageService blobStorageService)
    {
        _session = session;
        _blobStorageService = blobStorageService;
    }

    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _session.Query<Product>()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.Company.Id == request.CompanyId, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException($"No se encontró el producto {request.ProductId} para esta empresa.");
        }

        using var transaction = _session.BeginTransaction();
        await _session.DeleteAsync(product, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        if (!string.IsNullOrEmpty(product.SheetPdfBlobKey))
        {
            await _blobStorageService.DeleteAsync(product.SheetPdfBlobKey, cancellationToken);
        }
    }
}
