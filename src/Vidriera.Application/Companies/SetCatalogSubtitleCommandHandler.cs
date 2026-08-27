using MediatR;
using NHibernate;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Companies;

public class SetCatalogSubtitleCommandHandler : IRequestHandler<SetCatalogSubtitleCommand>
{
    private readonly ISession _session;

    public SetCatalogSubtitleCommandHandler(ISession session)
    {
        _session = session;
    }

    public async Task Handle(SetCatalogSubtitleCommand request, CancellationToken cancellationToken)
    {
        var company = await _session.GetOrThrowAsync<Company>(
            request.CompanyId,
            $"No existe la empresa {request.CompanyId}.",
            cancellationToken);

        var subtitle = request.Subtitle?.Trim();
        company.CatalogSubtitle = string.IsNullOrEmpty(subtitle) ? null : subtitle;

        await _session.UpdateInTransactionAsync(company, cancellationToken);
    }
}
