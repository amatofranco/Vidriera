using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Admin;

public class SetCompanySettingsCommandHandler : IRequestHandler<SetCompanySettingsCommand>
{
    private readonly ISession _session;

    public SetCompanySettingsCommandHandler(ISession session)
    {
        _session = session;
    }

    public async Task Handle(SetCompanySettingsCommand request, CancellationToken cancellationToken)
    {
        var company = await _session.GetOrThrowAsync<Company>(
            request.CompanyId,
            ErrorMessages.CompanyNotFound(request.CompanyId),
            cancellationToken);

        var slug = CompanySlug.Normalize(request.Slug);
        if (slug is not null)
        {
            if (!CompanySlug.IsValid(slug))
            {
                throw new ValidationException(ErrorMessages.InvalidCompanySlug);
            }

            var slugTaken = await _session.Query<Company>()
                .AnyAsync(c => c.Slug == slug && c.Id != company.Id, cancellationToken);
            if (slugTaken)
            {
                throw new ValidationException(ErrorMessages.CompanySlugTaken(slug));
            }
        }

        var customDomain = CompanyCustomDomain.Normalize(request.CustomDomain);
        if (customDomain is not null)
        {
            if (!CompanyCustomDomain.IsValid(customDomain))
            {
                throw new ValidationException(ErrorMessages.InvalidCompanyCustomDomain);
            }

            var domainTaken = await _session.Query<Company>()
                .AnyAsync(c => c.CustomDomain == customDomain && c.Id != company.Id, cancellationToken);
            if (domainTaken)
            {
                throw new ValidationException(ErrorMessages.CompanyCustomDomainTaken(customDomain));
            }
        }

        company.ShowCode = request.ShowCode;
        company.ShowPrice = request.ShowPrice;
        company.ShowOrders = request.ShowOrders;
        company.CatalogSubtitle = request.CatalogSubtitle;
        company.Slug = slug;
        company.CustomDomain = customDomain;

        await _session.UpdateInTransactionAsync(company, cancellationToken);
    }
}
