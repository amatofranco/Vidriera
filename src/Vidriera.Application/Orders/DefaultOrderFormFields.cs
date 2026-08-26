namespace Vidriera.Application.Orders;

public static class DefaultOrderFormFields
{
    public static readonly IReadOnlyList<OrderFormFieldDto> Fields =
    [
        new(new Guid("00000000-0000-0000-0000-000000000001"), "Razón Social", OrderFieldTypes.Name, true, 0),
        new(new Guid("00000000-0000-0000-0000-000000000002"), "CUIT", OrderFieldTypes.Cuit, true, 1),
        new(new Guid("00000000-0000-0000-0000-000000000003"), "Email", OrderFieldTypes.Email, true, 2),
        new(new Guid("00000000-0000-0000-0000-000000000004"), "Provincia", OrderFieldTypes.Province, true, 3),
        new(new Guid("00000000-0000-0000-0000-000000000005"), "Ciudad", OrderFieldTypes.FreeText, true, 4),
        new(new Guid("00000000-0000-0000-0000-000000000006"), "Dirección de entrega", OrderFieldTypes.FreeText, false, 5),
        new(new Guid("00000000-0000-0000-0000-000000000007"), "Nombre del local", OrderFieldTypes.FreeText, false, 6),
        new(new Guid("00000000-0000-0000-0000-000000000008"), "Condición frente al IVA", OrderFieldTypes.VatCondition, false, 7),
        new(new Guid("00000000-0000-0000-0000-000000000009"), "Teléfono", OrderFieldTypes.FreeText, false, 8),
        new(new Guid("00000000-0000-0000-0000-00000000000a"), "Expreso", OrderFieldTypes.FreeText, false, 9),
    ];
}
