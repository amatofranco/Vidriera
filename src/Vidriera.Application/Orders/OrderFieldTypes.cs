namespace Vidriera.Application.Orders;

public static class OrderFieldTypes
{
    public const string FreeText = "FreeText";
    public const string Name = "Name";
    public const string Email = "Email";
    public const string Cuit = "Cuit";
    public const string Province = "Province";
    public const string VatCondition = "VatCondition";

    public static readonly IReadOnlyList<string> All = [FreeText, Name, Email, Cuit, Province, VatCondition];

    public static bool IsValid(string fieldType) => All.Contains(fieldType);
}
