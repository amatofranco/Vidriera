using ClosedXML.Excel;
using Vidriera.Application.Abstractions;

namespace Vidriera.Infrastructure.Excel;

public class ClosedXmlPriceImportService : IPriceImportService
{
    public IReadOnlyList<PriceImportRow> ParsePriceRows(Stream fileContent)
    {
        using var workbook = new XLWorkbook(fileContent);
        var sheet = workbook.Worksheets.First();
        var rows = new List<PriceImportRow>();

        var usedRows = sheet.RangeUsed()?.RowsUsed() ?? Enumerable.Empty<IXLRangeRow>();
        foreach (var row in usedRows.Skip(1))
        {
            var code = row.Cell(1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                continue;
            }

            if (!row.Cell(2).TryGetValue(out decimal price))
            {
                continue;
            }

            rows.Add(new PriceImportRow(code, price));
        }

        return rows;
    }

    public byte[] GenerateTemplate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Precios");

        sheet.Cell(1, 1).Value = "Código";
        sheet.Cell(1, 2).Value = "Precio";
        sheet.Range(1, 1, 1, 2).Style.Font.Bold = true;
        sheet.Columns(1, 2).AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
