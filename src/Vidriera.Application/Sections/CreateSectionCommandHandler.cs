using MediatR;
using NHibernate;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Sections;

public class CreateSectionCommandHandler : IRequestHandler<CreateSectionCommand, SectionDto>
{
    private readonly ISession _session;
    private readonly IBlobStorageService _blobStorageService;

    public CreateSectionCommandHandler(ISession session, IBlobStorageService blobStorageService)
    {
        _session = session;
        _blobStorageService = blobStorageService;
    }

    public async Task<SectionDto> Handle(CreateSectionCommand request, CancellationToken cancellationToken)
    {
        var company = await _session.GetOrThrowAsync<Company>(
            request.CompanyId,
            $"No existe la empresa {request.CompanyId}.",
            cancellationToken);

        var name = string.IsNullOrWhiteSpace(request.Name)
            ? Path.GetFileNameWithoutExtension(request.OriginalFileName)
            : request.Name;

        Section? parent = null;
        if (request.ParentSectionId.HasValue)
        {
            parent = await _session.Query<Section>().GetOrThrowAsync(
                s => s.Id == request.ParentSectionId.Value && s.Company.Id == request.CompanyId,
                ErrorMessages.SectionNotFound(request.ParentSectionId.Value),
                cancellationToken);

            if (parent.ParentSection is not null)
            {
                throw new ValidationException(ErrorMessages.SectionCannotNestFurther);
            }
        }

        var nextSortOrder = parent is null
            ? await TopLevelOrdering.NextTopLevelSortOrderAsync(_session, request.CompanyId, cancellationToken)
            : await TopLevelOrdering.NextSectionSortOrderAsync(_session, parent.Id, cancellationToken);

        var section = new Section
        {
            Company = company,
            Name = name,
            SortOrder = nextSortOrder,
            ParentSection = parent
        };

        using var transaction = _session.BeginTransaction();

        await _session.SaveAsync(section, cancellationToken);

        var blobKey = BlobKeys.SectionCover(request.CompanyId, section.Id);
        await _blobStorageService.UploadAsync(blobKey, request.FileContent, "application/pdf", cancellationToken);

        section.CoverPdfBlobKey = blobKey;
        section.CoverPdfOriginalName = request.OriginalFileName;

        await transaction.CommitAsync(cancellationToken);

        return new SectionDto(section.Id, section.Name, section.SortOrder, section.ParentSection?.Id);
    }
}
