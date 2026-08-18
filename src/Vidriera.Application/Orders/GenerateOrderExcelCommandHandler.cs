using System.Text.Json;
using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Orders;

public class GenerateOrderExcelCommandHandler : IRequestHandler<GenerateOrderExcelCommand, OrderExcelResult>
{
    private readonly ISession _session;
    private readonly IExcelOrderService _excelOrderService;

    public GenerateOrderExcelCommandHandler(ISession session, IExcelOrderService excelOrderService)
    {
        _session = session;
        _excelOrderService = excelOrderService;
    }

    public async Task<OrderExcelResult> Handle(GenerateOrderExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0 || request.Items.Any(i => i.Quantity <= 0))
        {
            throw new ValidationException(ErrorMessages.MustSelectAtLeastOneOrderItem);
        }

        ValidateCustomer(request.Customer);

        var company = await _session.GetOrThrowAsync<Company>(
            request.CompanyId,
            ErrorMessages.CompanyNotFound(request.CompanyId),
            cancellationToken);

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _session.Query<Product>()
            .Where(p => p.Company.Id == request.CompanyId && productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        if (products.Count != productIds.Count)
        {
            throw new ValidationException(ErrorMessages.OrderContainsInvalidProducts);
        }

        var productsById = products.ToDictionary(p => p.Id);
        var lines = request.Items
            .Select(i => new OrderExcelLine(
                productsById[i.ProductId].Name,
                productsById[i.ProductId].Isbn,
                i.Quantity))
            .ToList();

        var order = new Order
        {
            Company = company,
            BusinessName = request.Customer.BusinessName,
            StoreName = request.Customer.StoreName,
            Cuit = request.Customer.Cuit,
            VatCondition = request.Customer.VatCondition,
            Phone = request.Customer.Phone,
            Email = request.Customer.Email,
            City = request.Customer.City,
            Province = request.Customer.Province,
            Carrier = request.Customer.Carrier,
            DeliveryAddress = request.Customer.DeliveryAddress,
            ItemsSnapshotJson = JsonSerializer.Serialize(lines),
            CreatedAt = DateTime.UtcNow,
        };
        await _session.SaveInTransactionAsync(order, cancellationToken);

        var content = _excelOrderService.GenerateOrderWorkbook(company.Name, request.Customer, lines);
        var fileName = $"{OrderLabels.DefaultFileNamePrefix}_{Sanitize(request.Customer.StoreName)}_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx";

        return new OrderExcelResult(content, fileName);
    }

    private static void ValidateCustomer(CustomerOrderInfo customer)
    {
        var requiredFields = new[]
        {
            customer.BusinessName,
            customer.Cuit,
            customer.Email
        };

        if (requiredFields.Any(string.IsNullOrWhiteSpace))
        {
            throw new ValidationException(ErrorMessages.OrderCustomerDataIncomplete);
        }
    }

    private static string Sanitize(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Where(c => !invalidChars.Contains(c)).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? OrderLabels.DefaultFileNamePrefix : sanitized.Replace(' ', '_');
    }
}
