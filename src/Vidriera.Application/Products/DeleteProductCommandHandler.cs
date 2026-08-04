using MediatR;
using NHibernate;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
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
        var product = await _session.Query<Product>().GetOrThrowAsync(
            p => p.Id == request.ProductId && p.Company.Id == request.CompanyId,
            $"No se encontró el producto {request.ProductId} para esta empresa.",
            cancellationToken);

        await _session.DeleteInTransactionAsync(product, cancellationToken);

        if (!string.IsNullOrEmpty(product.SheetPdfBlobKey))
        {
            await _blobStorageService.DeleteAsync(product.SheetPdfBlobKey, cancellationToken);
        }
    }
}
