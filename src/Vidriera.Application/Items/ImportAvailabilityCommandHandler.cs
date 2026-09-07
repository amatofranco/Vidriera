using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Items;

public class ImportAvailabilityCommandHandler : IRequestHandler<ImportAvailabilityCommand, ImportAvailabilityResult>
{
    private const string OutOfStockValue = "Agotado";

    private readonly ISession _session;
    private readonly IAvailabilityImportService _availabilityImportService;

    public ImportAvailabilityCommandHandler(ISession session, IAvailabilityImportService availabilityImportService)
    {
        _session = session;
        _availabilityImportService = availabilityImportService;
    }

    public async Task<ImportAvailabilityResult> Handle(ImportAvailabilityCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<AvailabilityImportRow> rows;
        try
        {
            rows = _availabilityImportService.ParseAvailabilityRows(request.FileContent);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new ValidationException(ErrorMessages.AvailabilityImportInvalidFile);
        }

        if (rows.Count == 0)
        {
            throw new ValidationException(ErrorMessages.AvailabilityImportEmpty);
        }

        var items = await _session.Query<Item>()
            .Where(p => p.Company.Id == request.CompanyId && p.Code != null)
            .ToListAsync(cancellationToken);

        var itemsByCode = items
            .GroupBy(p => p.Code!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var notFoundCodes = new List<string>();
        var markedOutOfStockCount = 0;

        using var transaction = _session.BeginTransaction();

        foreach (var row in rows)
        {
            if (!string.Equals(row.Availability, OutOfStockValue, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (itemsByCode.TryGetValue(row.Code, out var matches))
            {
                foreach (var item in matches)
                {
                    item.HasStock = false;
                    await _session.UpdateAsync(item, cancellationToken);
                    markedOutOfStockCount++;
                }
            }
            else
            {
                notFoundCodes.Add(row.Code);
            }
        }

        await transaction.CommitAsync(cancellationToken);

        return new ImportAvailabilityResult(markedOutOfStockCount, notFoundCodes);
    }
}
