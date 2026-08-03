namespace Vidriera.Application.Products;

public record ProductDto(Guid Id, string Name, string? Code, bool HasStock, bool HasSheet, Guid? SectionId, int SortOrder);
