using MediatR;
using NHibernate;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Companies;

public class UpdateCompanyLogoCommandHandler : IRequestHandler<UpdateCompanyLogoCommand>
{
    private readonly ISession _session;
    private readonly IBlobStorageService _blobStorageService;

    public UpdateCompanyLogoCommandHandler(ISession session, IBlobStorageService blobStorageService)
    {
        _session = session;
        _blobStorageService = blobStorageService;
    }

    public async Task Handle(UpdateCompanyLogoCommand request, CancellationToken cancellationToken)
    {
        var company = await _session.GetAsync<Company>(request.CompanyId, cancellationToken);
        if (company is null)
        {
            throw new NotFoundException($"No existe la empresa {request.CompanyId}.");
        }

        var blobKey = $"companies/{request.CompanyId}/logo";
        await _blobStorageService.UploadAsync(blobKey, request.FileContent, request.ContentType, cancellationToken);

        company.LogoBlobKey = blobKey;
        company.LogoContentType = request.ContentType;

        using var transaction = _session.BeginTransaction();
        await _session.UpdateAsync(company, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
