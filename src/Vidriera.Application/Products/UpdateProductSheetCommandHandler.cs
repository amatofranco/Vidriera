using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common.Exceptions;
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
        var product = await _session.Query<Product>()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.Company.Id == request.CompanyId, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException($"No se encontró el producto {request.ProductId} para esta empresa.");
        }

        var blobKey = $"companies/{request.CompanyId}/products/{request.ProductId}/{Guid.NewGuid()}.pdf";
        await _blobStorageService.UploadAsync(blobKey, request.FileContent, "application/pdf", cancellationToken);

        product.SheetPdfBlobKey = blobKey;
        product.SheetPdfOriginalName = request.OriginalFileName;

        using var transaction = _session.BeginTransaction();
        await _session.UpdateAsync(product, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
