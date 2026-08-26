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

        var company = await _session.GetOrThrowAsync<Company>(
            request.CompanyId,
            ErrorMessages.CompanyNotFound(request.CompanyId),
            cancellationToken);

        if (!company.ShowOrders)
        {
            throw new ValidationException(ErrorMessages.OrdersNotEnabled);
        }

        var customerSnapshot = await ValidateAndBuildCustomerSnapshotAsync(
            _session, request.CompanyId, request.CustomerFields, cancellationToken);

        var itemIds = request.Items.Select(i => i.ItemId).Distinct().ToList();
        var items = await _session.Query<Item>()
            .Where(p => p.Company.Id == request.CompanyId && itemIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        if (items.Count != itemIds.Count)
        {
            throw new ValidationException(ErrorMessages.OrderContainsInvalidItems);
        }

        var itemsById = items.ToDictionary(p => p.Id);
        var lines = request.Items
            .Select(i => new OrderExcelLine(
                itemsById[i.ItemId].Name,
                itemsById[i.ItemId].Code,
                i.Quantity,
                request.ShowPrices && company.ShowPrice ? itemsById[i.ItemId].Price : null))
            .ToList();

        var order = new Order
        {
            Company = company,
            ItemsSnapshotJson = JsonSerializer.Serialize(lines),
            CustomerFieldsJson = JsonSerializer.Serialize(customerSnapshot),
            CreatedAt = DateTime.UtcNow,
        };
        await _session.SaveInTransactionAsync(order, cancellationToken);

        var content = _excelOrderService.GenerateOrderWorkbook(company.Name, customerSnapshot, lines);
        var fileName = OrderExcelFileName.Build(DateTime.UtcNow);

        return new OrderExcelResult(content, fileName);
    }

    private static async Task<IReadOnlyList<CustomerFieldSnapshotEntry>> ValidateAndBuildCustomerSnapshotAsync(
        ISession session,
        Guid companyId,
        IReadOnlyList<CustomerFieldSubmission> submissions,
        CancellationToken cancellationToken)
    {
        var fields = await OrderFormFieldResolver.ResolveAsync(session, companyId, cancellationToken);
        var valuesByFieldId = submissions.ToDictionary(s => s.FieldId, s => (s.Value ?? "").Trim());

        var snapshot = new List<CustomerFieldSnapshotEntry>();
        foreach (var field in fields)
        {
            var value = valuesByFieldId.TryGetValue(field.Id, out var v) ? v : "";

            if (field.IsRequired && string.IsNullOrWhiteSpace(value))
            {
                throw new ValidationException(ErrorMessages.OrderCustomerDataIncomplete);
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                switch (field.FieldType)
                {
                    case OrderFieldTypes.Cuit when !CuitValidation.IsValid(value):
                        throw new ValidationException(ErrorMessages.InvalidCuit);
                    case OrderFieldTypes.Name when !NameValidation.IsValid(value):
                        throw new ValidationException(ErrorMessages.InvalidName);
                    case OrderFieldTypes.Email when !EmailValidation.IsValid(value):
                        throw new ValidationException(ErrorMessages.InvalidEmail);
                    case OrderFieldTypes.Phone when !PhoneValidation.IsValid(value):
                        throw new ValidationException(ErrorMessages.InvalidPhone);
                }
            }

            snapshot.Add(new CustomerFieldSnapshotEntry(field.Label, value));
        }

        return snapshot;
    }
}
