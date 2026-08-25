using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Items;

public class ImportPricesCommandHandler : IRequestHandler<ImportPricesCommand, ImportPricesResult>
{
    private readonly ISession _session;
    private readonly IPriceImportService _priceImportService;

    public ImportPricesCommandHandler(ISession session, IPriceImportService priceImportService)
    {
        _session = session;
        _priceImportService = priceImportService;
    }

    public async Task<ImportPricesResult> Handle(ImportPricesCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<PriceImportRow> rows;
        try
        {
            rows = _priceImportService.ParsePriceRows(request.FileContent);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new ValidationException(ErrorMessages.PriceImportInvalidFile);
        }

        if (rows.Count == 0)
        {
            throw new ValidationException(ErrorMessages.PriceImportEmpty);
        }

        var items = await _session.Query<Item>()
            .Where(p => p.Company.Id == request.CompanyId && p.Code != null)
            .ToListAsync(cancellationToken);

        var itemsByCode = items
            .GroupBy(p => p.Code!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var notFoundCodes = new List<string>();
        var updatedCount = 0;

        using var transaction = _session.BeginTransaction();

        foreach (var row in rows)
        {
            if (itemsByCode.TryGetValue(row.Code, out var matches))
            {
                foreach (var item in matches)
                {
                    item.Price = row.Price;
                    await _session.UpdateAsync(item, cancellationToken);
                    updatedCount++;
                }
            }
            else
            {
                notFoundCodes.Add(row.Code);
            }
        }

        await transaction.CommitAsync(cancellationToken);

        return new ImportPricesResult(updatedCount, notFoundCodes);
    }
}
