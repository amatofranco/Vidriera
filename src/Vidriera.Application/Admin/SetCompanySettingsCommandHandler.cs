using MediatR;
using NHibernate;
using Vidriera.Application.Common;
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

        company.ShowCode = request.ShowCode;
        company.ShowPrice = request.ShowPrice;
        company.ShowOrders = request.ShowOrders;

        await _session.UpdateInTransactionAsync(company, cancellationToken);
    }
}
