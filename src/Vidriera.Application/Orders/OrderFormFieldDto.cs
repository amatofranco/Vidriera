namespace Vidriera.Application.Orders;

public record OrderFormFieldDto(Guid Id, string Label, string FieldType, bool IsRequired, int SortOrder);
