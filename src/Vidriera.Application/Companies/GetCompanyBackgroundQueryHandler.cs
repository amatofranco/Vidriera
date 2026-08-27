using MediatR;
using NHibernate;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Companies;

public class GetCompanyBackgroundQueryHandler : IRequestHandler<GetCompanyBackgroundQuery, CompanyLogoResult>
{
    private readonly ISession _session;
    private readonly IBlobStorageService _blobStorageService;

    public GetCompanyBackgroundQueryHandler(ISession session, IBlobStorageService blobStorageService)
    {
        _session = session;
        _blobStorageService = blobStorageService;
    }

    public async Task<CompanyLogoResult> Handle(GetCompanyBackgroundQuery request, CancellationToken cancellationToken)
    {
        var company = await _session.GetAsync<Company>(request.CompanyId, cancellationToken);

        if (company?.BackgroundBlobKey is null)
        {
            throw new NotFoundException(ErrorMessages.CompanyLogoNotFound(request.CompanyId));
        }

        var content = await _blobStorageService.DownloadAsync(company.BackgroundBlobKey, cancellationToken);
        return new CompanyLogoResult(content, company.BackgroundContentType ?? "application/octet-stream");
    }
}
