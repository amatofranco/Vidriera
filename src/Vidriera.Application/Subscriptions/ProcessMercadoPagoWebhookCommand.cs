using MediatR;

namespace Vidriera.Application.Subscriptions;

public record ProcessMercadoPagoWebhookCommand(string Type, string ResourceId) : IRequest;
