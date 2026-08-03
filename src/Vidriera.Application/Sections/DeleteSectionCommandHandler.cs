using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
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
        var section = await _session.Query<Section>()
            .FirstOrDefaultAsync(s => s.Id == request.SectionId && s.Company.Id == request.CompanyId, cancellationToken);

        if (section is null)
        {
            throw new NotFoundException($"No se encontró la carátula {request.SectionId} para esta empresa.");
        }

        // Members are detached, not deleted -- they go back to being loose products,
        // appended after whatever is currently at the end of the top-level order,
        // keeping their relative order to each other.
        var members = await _session.Query<Product>()
            .Where(p => p.Section != null && p.Section.Id == request.SectionId)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(cancellationToken);

        var nextTopLevelSortOrder = await TopLevelOrdering.NextTopLevelSortOrderAsync(_session, request.CompanyId, cancellationToken);

        using var transaction = _session.BeginTransaction();

        foreach (var member in members)
        {
            member.Section = null;
            member.SortOrder = nextTopLevelSortOrder++;
            await _session.UpdateAsync(member, cancellationToken);
        }

        await _session.DeleteAsync(section, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await _blobStorageService.DeleteAsync(section.CoverPdfBlobKey, cancellationToken);
    }
}
