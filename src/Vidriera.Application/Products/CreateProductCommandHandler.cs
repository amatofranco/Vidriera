using MediatR;
using NHibernate;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common.Exceptions;
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
        var company = await _session.GetAsync<Company>(request.CompanyId, cancellationToken);
        if (company is null)
        {
            throw new NotFoundException($"No existe la empresa {request.CompanyId}.");
        }

        var name = string.IsNullOrWhiteSpace(request.Name)
            ? Path.GetFileNameWithoutExtension(request.OriginalFileName)
            : request.Name;

        var product = new Product
        {
            Company = company,
            Name = name,
            HasStock = false,
            IsActive = true
        };

        using var transaction = _session.BeginTransaction();

        await _session.SaveAsync(product, cancellationToken);

        var blobKey = $"companies/{request.CompanyId}/products/{product.Id}/{Guid.NewGuid()}.pdf";
        await _blobStorageService.UploadAsync(blobKey, request.FileContent, "application/pdf", cancellationToken);

        product.SheetPdfBlobKey = blobKey;
        product.SheetPdfOriginalName = request.OriginalFileName;

        await transaction.CommitAsync(cancellationToken);

        return new ProductDto(product.Id, product.Name, product.Code, product.HasStock, HasSheet: true);
    }
}
