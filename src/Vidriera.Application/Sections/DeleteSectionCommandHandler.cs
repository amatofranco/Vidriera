using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Sections;

public class DeleteSectionCommandHandler : IRequestHandler<DeleteSectionCommand>
{
    private readonly ISession _session;
    private readonly IBlobStorageService _blobStorageService;

    public DeleteSectionCommandHandler(ISession session, IBlobStorageService blobStorageService)
    {
        _session = session;
        _blobStorageService = blobStorageService;
    }

    public async Task Handle(DeleteSectionCommand request, CancellationToken cancellationToken)
    {
        var section = await _session.Query<Section>().GetOrThrowAsync(
            s => s.Id == request.SectionId && s.Company.Id == request.CompanyId,
            ErrorMessages.SectionNotFound(request.SectionId),
            cancellationToken);

        var members = await _session.Query<Product>()
            .Where(p => p.Section != null && p.Section.Id == request.SectionId)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(cancellationToken);

        var childSections = await _session.Query<Section>()
            .Where(s => s.ParentSection != null && s.ParentSection.Id == request.SectionId)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(cancellationToken);

        var nextTopLevelSortOrder = await TopLevelOrdering.NextTopLevelSortOrderAsync(_session, request.CompanyId, cancellationToken);

        using var transaction = _session.BeginTransaction();

        foreach (var child in childSections)
        {
            child.ParentSection = null;
            child.SortOrder = nextTopLevelSortOrder++;
            await _session.UpdateAsync(child, cancellationToken);
        }

        foreach (var member in members)
        {
            member.Section = null;
            member.SortOrder = nextTopLevelSortOrder++;
            await _session.UpdateAsync(member, cancellationToken);
        }

        await _session.DeleteAsync(section, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        if (!string.IsNullOrEmpty(section.CoverPdfBlobKey))
        {
            await _blobStorageService.DeleteAsync(section.CoverPdfBlobKey, cancellationToken);
        }
    }
}
