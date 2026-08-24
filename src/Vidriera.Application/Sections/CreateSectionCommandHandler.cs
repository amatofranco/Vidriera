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
    private readonly IPdfMergeService _pdfMergeService;

    public CreateSectionCommandHandler(ISession session, IBlobStorageService blobStorageService, IPdfMergeService pdfMergeService)
    {
        _session = session;
        _blobStorageService = blobStorageService;
        _pdfMergeService = pdfMergeService;
    }

    public async Task<SectionDto> Handle(CreateSectionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) && request.FileContent is null)
        {
            throw new ValidationException(ErrorMessages.SectionNameRequired);
        }

        var company = await _session.GetOrThrowAsync<Company>(
            request.CompanyId,
            $"No existe la empresa {request.CompanyId}.",
            cancellationToken);

        MemoryStream? validatedFileContent = null;
        if (request.FileContent is not null)
        {
            validatedFileContent = await PdfUploadValidation.BufferAndValidatePageCountAsync(
                request.FileContent, _pdfMergeService, cancellationToken);
        }

        var name = string.IsNullOrWhiteSpace(request.Name)
            ? Path.GetFileNameWithoutExtension(request.OriginalFileName)!
            : request.Name;

        var parent = await SectionNesting.ResolveParentAsync(_session, request.CompanyId, request.ParentSectionId, null, cancellationToken);

        var sortOrder = parent is null
            ? await TopLevelOrdering.PrependTopLevelSortOrderAsync(_session, request.CompanyId, cancellationToken)
            : await TopLevelOrdering.NextSectionSortOrderAsync(_session, parent.Id, cancellationToken);

        var section = new Section
        {
            Company = company,
            Name = name,
            SortOrder = sortOrder,
            ParentSection = parent
        };

        using var transaction = _session.BeginTransaction();

        await _session.SaveAsync(section, cancellationToken);

        if (validatedFileContent is not null)
        {
            var blobKey = BlobKeys.SectionCover(request.CompanyId, section.Id);
            await _blobStorageService.UploadAsync(blobKey, validatedFileContent, "application/pdf", cancellationToken);

            section.CoverPdfBlobKey = blobKey;
            section.CoverPdfOriginalName = request.OriginalFileName;

            await validatedFileContent.DisposeAsync();
        }

        await transaction.CommitAsync(cancellationToken);

        return new SectionDto(section.Id, section.Name, section.SortOrder, section.ParentSection?.Id);
    }
}
