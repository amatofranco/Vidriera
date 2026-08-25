using MediatR;
using NHibernate;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Items;

public class DeleteItemCommandHandler : IRequestHandler<DeleteItemCommand>
{
    private readonly ISession _session;
    private readonly IBlobStorageService _blobStorageService;

    public DeleteItemCommandHandler(ISession session, IBlobStorageService blobStorageService)
    {
        _session = session;
        _blobStorageService = blobStorageService;
    }

    public async Task Handle(DeleteItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _session.Query<Item>().GetOrThrowAsync(
            p => p.Id == request.ItemId && p.Company.Id == request.CompanyId,
            ErrorMessages.ItemNotFound(request.ItemId),
            cancellationToken);

        try
        {
            await _session.DeleteInTransactionAsync(item, cancellationToken);
        }
        catch (StaleStateException)
        {
            return;
        }

        if (!string.IsNullOrEmpty(item.SheetPdfBlobKey))
        {
            await _blobStorageService.DeleteAsync(item.SheetPdfBlobKey, cancellationToken);
        }
    }
}
