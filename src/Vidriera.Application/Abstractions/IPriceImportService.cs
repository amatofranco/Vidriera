namespace Vidriera.Application.Abstractions;

public record PriceImportRow(string Code, decimal Price);

public interface IPriceImportService
{
    IReadOnlyList<PriceImportRow> ParsePriceRows(Stream fileContent);
}
