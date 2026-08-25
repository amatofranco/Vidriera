using MediatR;
using NHibernate;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Application.Subscriptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Items;

public class CreateItemCommandHandler : IRequestHandler<CreateItemCommand, ItemDto>
{
    private readonly ISession _session;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IPdfMergeService _pdfMergeService;

    public CreateItemCommandHandler(ISession session, IBlobStorageService blobStorageService, IPdfMergeService pdfMergeService)
    {
        _session = session;
        _blobStorageService = blobStorageService;
        _pdfMergeService = pdfMergeService;
    }

    public async Task<ItemDto> Handle(CreateItemCommand request, CancellationToken cancellationToken)
    {
        var company = await _session.GetOrThrowAsync<Company>(
            request.CompanyId,
            $"No existe la empresa {request.CompanyId}.",
            cancellationToken);

        await PlanLimitEnforcer.EnsureCanAddItemAsync(_session, request.CompanyId, cancellationToken);

        await using var validatedFileContent = await PdfUploadValidation.BufferAndValidatePageCountAsync(
            request.FileContent, _pdfMergeService, cancellationToken);

        var name = string.IsNullOrWhiteSpace(request.Name)
            ? Path.GetFileNameWithoutExtension(request.OriginalFileName)
            : request.Name;

        var nextSortOrder = await TopLevelOrdering.PrependTopLevelSortOrderAsync(_session, request.CompanyId, cancellationToken);

        var code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();

        var item = new Item
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

        await _session.SaveAsync(item, cancellationToken);

        var blobKey = BlobKeys.ItemSheet(request.CompanyId, item.Id);
        await _blobStorageService.UploadAsync(blobKey, validatedFileContent, "application/pdf", cancellationToken);

        item.SheetPdfBlobKey = blobKey;
        item.SheetPdfOriginalName = request.OriginalFileName;

        await transaction.CommitAsync(cancellationToken);

        return new ItemDto(item.Id, item.Name, item.HasStock, HasSheet: true, SectionId: null, item.SortOrder, item.Code, item.Price);
    }
}
