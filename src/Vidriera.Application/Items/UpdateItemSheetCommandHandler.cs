using MediatR;
using NHibernate;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Application.Subscriptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Items;

public class UpdateItemSheetCommandHandler : IRequestHandler<UpdateItemSheetCommand>
{
    private readonly ISession _session;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IPdfMergeService _pdfMergeService;

    public UpdateItemSheetCommandHandler(ISession session, IBlobStorageService blobStorageService, IPdfMergeService pdfMergeService)
    {
        _session = session;
        _blobStorageService = blobStorageService;
        _pdfMergeService = pdfMergeService;
    }

    public async Task Handle(UpdateItemSheetCommand request, CancellationToken cancellationToken)
    {
        var item = await _session.Query<Item>().GetOrThrowAsync(
            p => p.Id == request.ItemId && p.Company.Id == request.CompanyId,
            ErrorMessages.ItemNotFound(request.ItemId),
            cancellationToken);

        var (validatedFileContent, pageCount) = await PdfUploadValidation.BufferAndGetPageCountAsync(
            request.FileContent, _pdfMergeService, cancellationToken);
        await using var _ = validatedFileContent;

        await PlanLimitEnforcer.EnsureCanAddPagesAsync(_session, request.CompanyId, pageCount - item.PageCount, cancellationToken);

        var blobKey = BlobKeys.ItemSheet(request.CompanyId, request.ItemId);
        await _blobStorageService.UploadAsync(blobKey, validatedFileContent, "application/pdf", cancellationToken);

        item.SheetPdfBlobKey = blobKey;
        item.SheetPdfOriginalName = request.OriginalFileName;
        item.PageCount = pageCount;

        await _session.UpdateInTransactionAsync(item, cancellationToken);
    }
}
