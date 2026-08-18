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
            ("Nombre del local", customer.StoreName),
            ("CUIT", customer.Cuit),
            ("Condición frente al IVA", customer.VatCondition),
            ("Teléfono", customer.Phone),
            ("Email", customer.Email),
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

        var headerRow = row;
        sheet.Cell(headerRow, 1).Value = "Producto";
        sheet.Cell(headerRow, 2).Value = "ISBN";
        sheet.Cell(headerRow, 3).Value = "Cantidad";
        sheet.Range(headerRow, 1, headerRow, 3).Style.Font.Bold = true;
        row++;

        foreach (var line in lines)
        {
            sheet.Cell(row, 1).Value = line.ProductName;
            sheet.Cell(row, 2).Value = line.Isbn ?? "";
            sheet.Cell(row, 3).Value = line.Quantity;
            row++;
        }

        sheet.Columns(1, 3).AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
