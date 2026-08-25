using MediatR;
using Vidriera.Application.Abstractions;

namespace Vidriera.Application.Subscriptions;

public record GetPreapprovalDetailsQuery(string PreapprovalId) : IRequest<MercadoPagoPreapproval>;
