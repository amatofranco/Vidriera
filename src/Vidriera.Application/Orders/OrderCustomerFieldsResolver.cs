using System.Text.Json;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Orders;

internal static class OrderCustomerFieldsResolver
{
    public static IReadOnlyList<CustomerFieldSnapshotEntry> Resolve(Order order)
    {
        if (!string.IsNullOrWhiteSpace(order.CustomerFieldsJson))
        {
            var parsed = JsonSerializer.Deserialize<List<CustomerFieldSnapshotEntry>>(order.CustomerFieldsJson);
            if (parsed is { Count: > 0 })
            {
                return parsed;
            }
        }

        return BuildFromLegacyColumns(order);
    }

    private static IReadOnlyList<CustomerFieldSnapshotEntry> BuildFromLegacyColumns(Order order)
    {
        var fields = new List<CustomerFieldSnapshotEntry>();

        void Add(string label, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                fields.Add(new CustomerFieldSnapshotEntry(label, value));
            }
        }

        Add("Razón Social", order.BusinessName);
        Add("CUIT", order.Cuit);
        Add("Email", order.Email);
        Add("Provincia", order.Province);
        Add("Ciudad", order.City);
        Add("Dirección de entrega", order.DeliveryAddress);
        Add("Nombre del local", order.StoreName);
        Add("Condición frente al IVA", order.VatCondition);
        Add("Teléfono", order.Phone);
        Add("Expreso", order.Carrier);

        return fields;
    }
}
