using MediatR;

namespace Vidriera.Application.Orders;

public record OrderLineItem(Guid ItemId, int Quantity);

public record CustomerFieldSubmission(Guid FieldId, string Value);

public record CustomerFieldSnapshotEntry(string Label, string Value);

public record GenerateOrderExcelCommand(
    Guid CompanyId,
    IReadOnlyList<OrderLineItem> Items,
    IReadOnlyList<CustomerFieldSubmission> CustomerFields,
    bool ShowPrices = false) : IRequest<OrderExcelResult>;

public record OrderExcelResult(byte[] Content, string FileName);
