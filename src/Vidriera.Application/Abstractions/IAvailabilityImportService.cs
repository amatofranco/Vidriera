namespace Vidriera.Application.Abstractions;

public record AvailabilityImportRow(string Code, string? Availability);

public interface IAvailabilityImportService
{
    IReadOnlyList<AvailabilityImportRow> ParseAvailabilityRows(Stream fileContent);
    byte[] GenerateTemplate();
}
