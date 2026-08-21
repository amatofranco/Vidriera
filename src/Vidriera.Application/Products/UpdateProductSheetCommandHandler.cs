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
    private readonly IPdfMergeService _pdfMergeService;

    public UpdateProductSheetCommandHandler(ISession session, IBlobStorageService blobStorageService, IPdfMergeService pdfMergeService)
    {
        _session = session;
        _blobStorageService = blobStorageService;
        _pdfMergeService = pdfMergeService;
    }

    public async Task Handle(UpdateProductSheetCommand request, CancellationToken cancellationToken)
    {
        var product = await _session.Query<Product>().GetOrThrowAsync(
            p => p.Id == request.ProductId && p.Company.Id == request.CompanyId,
            ErrorMessages.ProductNotFound(request.ProductId),
            cancellationToken);

        await using var validatedFileContent = await PdfUploadValidation.BufferAndValidatePageCountAsync(
            request.FileContent, _pdfMergeService, cancellationToken);

        var blobKey = BlobKeys.ProductSheet(request.CompanyId, request.ProductId);
        await _blobStorageService.UploadAsync(blobKey, validatedFileContent, "application/pdf", cancellationToken);

        product.SheetPdfBlobKey = blobKey;
        product.SheetPdfOriginalName = request.OriginalFileName;

        await _session.UpdateInTransactionAsync(product, cancellationToken);
    }
}
