using MediatR;
using NHibernate;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Companies;

public class UpdateCompanyBackgroundCommandHandler : IRequestHandler<UpdateCompanyBackgroundCommand>
{
    private readonly ISession _session;
    private readonly IBlobStorageService _blobStorageService;

    public UpdateCompanyBackgroundCommandHandler(ISession session, IBlobStorageService blobStorageService)
    {
        _session = session;
        _blobStorageService = blobStorageService;
    }

    public async Task Handle(UpdateCompanyBackgroundCommand request, CancellationToken cancellationToken)
    {
        var company = await _session.GetOrThrowAsync<Company>(
            request.CompanyId,
            $"No existe la empresa {request.CompanyId}.",
            cancellationToken);

        var blobKey = BlobKeys.CompanyBackground(request.CompanyId);
        await _blobStorageService.UploadAsync(blobKey, request.FileContent, request.ContentType, cancellationToken);

        company.BackgroundBlobKey = blobKey;
        company.BackgroundContentType = request.ContentType;

        await _session.UpdateInTransactionAsync(company, cancellationToken);
    }
}
