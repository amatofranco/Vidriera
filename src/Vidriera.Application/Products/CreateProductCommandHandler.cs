using MediatR;
using NHibernate;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Application.Subscriptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Products;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly ISession _session;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IPdfMergeService _pdfMergeService;

    public CreateProductCommandHandler(ISession session, IBlobStorageService blobStorageService, IPdfMergeService pdfMergeService)
    {
        _session = session;
        _blobStorageService = blobStorageService;
        _pdfMergeService = pdfMergeService;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var company = await _session.GetOrThrowAsync<Company>(
            request.CompanyId,
            $"No existe la empresa {request.CompanyId}.",
            cancellationToken);

        await PlanLimitEnforcer.EnsureCanAddProductAsync(_session, request.CompanyId, cancellationToken);

        await using var validatedFileContent = await PdfUploadValidation.BufferAndValidatePageCountAsync(
            request.FileContent, _pdfMergeService, cancellationToken);

        var name = string.IsNullOrWhiteSpace(request.Name)
            ? Path.GetFileNameWithoutExtension(request.OriginalFileName)
            : request.Name;

        var nextSortOrder = await TopLevelOrdering.PrependTopLevelSortOrderAsync(_session, request.CompanyId, cancellationToken);

        var code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();

        var product = new Product
        {
            Company = company,
            Name = name,
            Code = code,
            Price = request.Price,
            HasStock = false,
            IsActive = true,
            SortOrder = nextSortOrder
        };

        using var transaction = _session.BeginTransaction();

        await _session.SaveAsync(product, cancellationToken);

        var blobKey = BlobKeys.ProductSheet(request.CompanyId, product.Id);
        await _blobStorageService.UploadAsync(blobKey, validatedFileContent, "application/pdf", cancellationToken);

        product.SheetPdfBlobKey = blobKey;
        product.SheetPdfOriginalName = request.OriginalFileName;

        await transaction.CommitAsync(cancellationToken);

        return new ProductDto(product.Id, product.Name, product.HasStock, HasSheet: true, SectionId: null, product.SortOrder, product.Code, product.Price);
    }
}
