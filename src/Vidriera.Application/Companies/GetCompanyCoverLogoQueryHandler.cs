using MediatR;
using NHibernate;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Companies;

public class GetCompanyCoverLogoQueryHandler : IRequestHandler<GetCompanyCoverLogoQuery, CompanyLogoResult>
{
    private readonly ISession _session;
    private readonly IBlobStorageService _blobStorageService;

    public GetCompanyCoverLogoQueryHandler(ISession session, IBlobStorageService blobStorageService)
    {
        _session = session;
        _blobStorageService = blobStorageService;
    }

    public async Task<CompanyLogoResult> Handle(GetCompanyCoverLogoQuery request, CancellationToken cancellationToken)
    {
        var company = await _session.GetAsync<Company>(request.CompanyId, cancellationToken);

        if (company?.CoverLogoBlobKey is null)
        {
            throw new NotFoundException(ErrorMessages.CompanyLogoNotFound(request.CompanyId));
        }

        var content = await _blobStorageService.DownloadAsync(company.CoverLogoBlobKey, cancellationToken);
        return new CompanyLogoResult(content, company.CoverLogoContentType ?? "application/octet-stream");
    }
}
