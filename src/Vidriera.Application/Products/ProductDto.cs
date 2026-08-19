namespace Vidriera.Application.Products;

public record ProductDto(Guid Id, string Name, bool HasStock, bool HasSheet, Guid? SectionId, int SortOrder, string? Code = null);
