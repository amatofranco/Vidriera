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
        var company = await _session.GetAsync<Company>(request.CompanyId, cancellationToken);
        if (company is null)
        {
            throw new NotFoundException($"No existe la empresa {request.CompanyId}.");
        }

        var name = string.IsNullOrWhiteSpace(request.Name)
            ? Path.GetFileNameWithoutExtension(request.OriginalFileName)
            : request.Name;

        var nextSortOrder = await TopLevelOrdering.NextTopLevelSortOrderAsync(_session, request.CompanyId, cancellationToken);

        var section = new Section
        {
            Company = company,
            Name = name,
            SortOrder = nextSortOrder
        };

        using var transaction = _session.BeginTransaction();

        await _session.SaveAsync(section, cancellationToken);

        var blobKey = $"companies/{request.CompanyId}/sections/{section.Id}/{Guid.NewGuid()}.pdf";
        await _blobStorageService.UploadAsync(blobKey, request.FileContent, "application/pdf", cancellationToken);

        section.CoverPdfBlobKey = blobKey;
        section.CoverPdfOriginalName = request.OriginalFileName;

        await transaction.CommitAsync(cancellationToken);

        return new SectionDto(section.Id, section.Name, section.SortOrder);
    }
}
