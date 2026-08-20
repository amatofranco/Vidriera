using ClosedXML.Excel;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Orders;

namespace Vidriera.Infrastructure.Excel;

public class ClosedXmlOrderService : IExcelOrderService
{
    public byte[] GenerateOrderWorkbook(string companyName, CustomerOrderInfo customer, IReadOnlyList<OrderExcelLine> lines)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Pedido");

        var row = 1;

        sheet.Cell(row, 1).Value = "Empresa";
        sheet.Cell(row, 2).Value = companyName;
        row++;

        sheet.Cell(row, 1).Value = "Fecha";
        sheet.Cell(row, 2).Value = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm");
        row += 2;

        var customerFields = new (string Label, string? Value)[]
        {
            ("Razón Social", customer.BusinessName),
            ("CUIT", customer.Cuit),
            ("Email", customer.Email),
            ("Nombre del local", customer.StoreName),
            ("Condición frente al IVA", customer.VatCondition),
            ("Teléfono", customer.Phone),
            ("Ciudad", customer.City),
            ("Provincia", customer.Province),
            ("Expreso", customer.Carrier),
            ("Dirección de entrega", customer.DeliveryAddress),
        };

        foreach (var (label, value) in customerFields)
        {
            sheet.Cell(row, 1).Value = label;
            sheet.Cell(row, 1).Style.Font.Bold = true;
            sheet.Cell(row, 2).Value = value ?? "";
            row++;
        }

        row++;

        var showPrices = lines.Count > 0 && lines.All(l => l.UnitPrice.HasValue);
        const string priceFormat = "$ #,##0.00";

        var headerRow = row;
        sheet.Cell(headerRow, 1).Value = "Producto";
        sheet.Cell(headerRow, 2).Value = "Código";
        sheet.Cell(headerRow, 3).Value = "Cantidad";
        if (showPrices)
        {
            sheet.Cell(headerRow, 4).Value = "Precio";
            sheet.Cell(headerRow, 5).Value = "Subtotal";
        }
        sheet.Range(headerRow, 1, headerRow, showPrices ? 5 : 3).Style.Font.Bold = true;
        row++;

        decimal total = 0;
        foreach (var line in lines)
        {
            sheet.Cell(row, 1).Value = line.ProductName;
            sheet.Cell(row, 2).Value = line.Code ?? "";
            sheet.Cell(row, 3).Value = line.Quantity;
            if (showPrices)
            {
                var subtotal = line.UnitPrice!.Value * line.Quantity;
                sheet.Cell(row, 4).Value = line.UnitPrice.Value;
                sheet.Cell(row, 4).Style.NumberFormat.Format = priceFormat;
                sheet.Cell(row, 5).Value = subtotal;
                sheet.Cell(row, 5).Style.NumberFormat.Format = priceFormat;
                total += subtotal;
            }
            row++;
        }

        if (showPrices)
        {
            sheet.Cell(row, 4).Value = "Total";
            sheet.Cell(row, 4).Style.Font.Bold = true;
            sheet.Cell(row, 5).Value = total;
            sheet.Cell(row, 5).Style.Font.Bold = true;
            sheet.Cell(row, 5).Style.NumberFormat.Format = priceFormat;
            row++;
        }

        sheet.Columns(1, showPrices ? 5 : 3).AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
