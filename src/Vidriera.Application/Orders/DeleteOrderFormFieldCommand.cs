using MediatR;

namespace Vidriera.Application.Orders;

public record DeleteOrderFormFieldCommand(Guid CompanyId, Guid FieldId) : IRequest;
