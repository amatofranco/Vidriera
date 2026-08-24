namespace Vidriera.Application.Abstractions;

public interface IExchangeRateService
{
    Task<decimal> GetUsdToArsOficialRateAsync(CancellationToken cancellationToken);
}
