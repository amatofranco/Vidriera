namespace Vidriera.Application.Items;

public record ItemDto(Guid Id, string Name, bool HasStock, bool HasSheet, Guid? SectionId, int SortOrder, string? Code = null, decimal? Price = null);
