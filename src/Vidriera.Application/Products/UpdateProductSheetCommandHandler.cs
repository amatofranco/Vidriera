using MediatR;
using NHibernate;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Products;

public class UpdateProductSheetCommandHandler : IRequestHandler<UpdateProductSheetCommand>
{
    private readonly ISession _session;
    private readonly IBlobStorageService _blobStorageService;

    public UpdateProductSheetCommandHandler(ISession session, IBlobStorageService blobStorageService)
    {
        _session = session;
        _blobStorageService = blobStorageService;
    }

    public async Task Handle(UpdateProductSheetCommand request, CancellationToken cancellationToken)
    {
        var product = await _session.Query<Product>().GetOrThrowAsync(
            p => p.Id == request.ProductId && p.Company.Id == request.CompanyId,
            $"No se encontró el producto {request.ProductId} para esta empresa.",
            cancellationToken);

        var blobKey = BlobKeys.ProductSheet(request.CompanyId, request.ProductId);
        await _blobStorageService.UploadAsync(blobKey, request.FileContent, "application/pdf", cancellationToken);

        product.SheetPdfBlobKey = blobKey;
        product.SheetPdfOriginalName = request.OriginalFileName;

        await _session.UpdateInTransactionAsync(product, cancellationToken);
    }
}
