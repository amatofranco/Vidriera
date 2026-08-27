using MediatR;
using NHibernate;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Companies;

public class DeleteCompanyBackgroundCommandHandler : IRequestHandler<DeleteCompanyBackgroundCommand>
{
    private readonly ISession _session;
    private readonly IBlobStorageService _blobStorageService;

    public DeleteCompanyBackgroundCommandHandler(ISession session, IBlobStorageService blobStorageService)
    {
        _session = session;
        _blobStorageService = blobStorageService;
    }

    public async Task Handle(DeleteCompanyBackgroundCommand request, CancellationToken cancellationToken)
    {
        var company = await _session.GetOrThrowAsync<Company>(
            request.CompanyId,
            $"No existe la empresa {request.CompanyId}.",
            cancellationToken);

        if (company.BackgroundBlobKey is null)
        {
            return;
        }

        await _blobStorageService.DeleteAsync(company.BackgroundBlobKey, cancellationToken);

        company.BackgroundBlobKey = null;
        company.BackgroundContentType = null;

        await _session.UpdateInTransactionAsync(company, cancellationToken);
    }
}
