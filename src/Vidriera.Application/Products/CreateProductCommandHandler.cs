using MediatR;
using NHibernate;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Products;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly ISession _session;
    private readonly IBlobStorageService _blobStorageService;

    public CreateProductCommandHandler(ISession session, IBlobStorageService blobStorageService)
    {
        _session = session;
        _blobStorageService = blobStorageService;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var company = await _session.GetOrThrowAsync<Company>(
            request.CompanyId,
            $"No existe la empresa {request.CompanyId}.",
            cancellationToken);

        var name = string.IsNullOrWhiteSpace(request.Name)
            ? Path.GetFileNameWithoutExtension(request.OriginalFileName)
            : request.Name;

        var nextSortOrder = await TopLevelOrdering.PrependTopLevelSortOrderAsync(_session, request.CompanyId, cancellationToken);

        var product = new Product
        {
            Company = company,
            Name = name,
            HasStock = false,
            IsActive = true,
            SortOrder = nextSortOrder
        };

        using var transaction = _session.BeginTransaction();

        await _session.SaveAsync(product, cancellationToken);

        var blobKey = BlobKeys.ProductSheet(request.CompanyId, product.Id);
        await _blobStorageService.UploadAsync(blobKey, request.FileContent, "application/pdf", cancellationToken);

        product.SheetPdfBlobKey = blobKey;
        product.SheetPdfOriginalName = request.OriginalFileName;

        await transaction.CommitAsync(cancellationToken);

        return new ProductDto(product.Id, product.Name, product.Code, product.HasStock, HasSheet: true, SectionId: null, product.SortOrder);
    }
}
