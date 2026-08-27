using MediatR;
using NHibernate;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Companies;

public class UpdateCompanyCoverLogoCommandHandler : IRequestHandler<UpdateCompanyCoverLogoCommand>
{
    private readonly ISession _session;
    private readonly IBlobStorageService _blobStorageService;

    public UpdateCompanyCoverLogoCommandHandler(ISession session, IBlobStorageService blobStorageService)
    {
        _session = session;
        _blobStorageService = blobStorageService;
    }

    public async Task Handle(UpdateCompanyCoverLogoCommand request, CancellationToken cancellationToken)
    {
        var company = await _session.GetOrThrowAsync<Company>(
            request.CompanyId,
            $"No existe la empresa {request.CompanyId}.",
            cancellationToken);

        var blobKey = BlobKeys.CompanyCoverLogo(request.CompanyId);
        await _blobStorageService.UploadAsync(blobKey, request.FileContent, request.ContentType, cancellationToken);

        company.CoverLogoBlobKey = blobKey;
        company.CoverLogoContentType = request.ContentType;

        await _session.UpdateInTransactionAsync(company, cancellationToken);
    }
}
