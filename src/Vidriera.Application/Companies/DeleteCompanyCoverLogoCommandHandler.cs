using MediatR;
using NHibernate;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Companies;

public class DeleteCompanyCoverLogoCommandHandler : IRequestHandler<DeleteCompanyCoverLogoCommand>
{
    private readonly ISession _session;
    private readonly IBlobStorageService _blobStorageService;

    public DeleteCompanyCoverLogoCommandHandler(ISession session, IBlobStorageService blobStorageService)
    {
        _session = session;
        _blobStorageService = blobStorageService;
    }

    public async Task Handle(DeleteCompanyCoverLogoCommand request, CancellationToken cancellationToken)
    {
        var company = await _session.GetOrThrowAsync<Company>(
            request.CompanyId,
            $"No existe la empresa {request.CompanyId}.",
            cancellationToken);

        if (company.CoverLogoBlobKey is null)
        {
            return;
        }

        await _blobStorageService.DeleteAsync(company.CoverLogoBlobKey, cancellationToken);

        company.CoverLogoBlobKey = null;
        company.CoverLogoContentType = null;

        await _session.UpdateInTransactionAsync(company, cancellationToken);
    }
}
