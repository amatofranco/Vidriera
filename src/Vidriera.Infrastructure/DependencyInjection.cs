using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NHibernate;
using Vidriera.Application.Abstractions;
using Vidriera.Infrastructure.Persistence;
using Vidriera.Infrastructure.Storage;
using Vidriera.Infrastructure.Pdf;
using Vidriera.Infrastructure.Auth;
using Vidriera.Infrastructure.Excel;
using Vidriera.Application.Subscriptions;
using Vidriera.Infrastructure.MercadoPago;
using Vidriera.Infrastructure.ExchangeRate;

namespace Vidriera.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Falta la connection string 'Postgres' en la configuración.");

        var sessionFactory = NHibernateSessionFactoryBuilder.BuildSessionFactory(connectionString);
        services.AddSingleton(sessionFactory);
        services.AddScoped(sp => sp.GetRequiredService<ISessionFactory>().OpenSession());

        services.Configure<R2Options>(configuration.GetSection("R2"));
        services.AddSingleton<IBlobStorageService, R2BlobStorageService>();

        services.AddSingleton<IPdfMergeService, PdfSharpMergeService>();
        services.AddSingleton<IPdfRasterizerService, PdfiumRasterizerService>();
        services.AddSingleton<IExcelOrderService, ClosedXmlOrderService>();
        services.AddSingleton<IPriceImportService, ClosedXmlPriceImportService>();

        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        services.Configure<MercadoPagoOptions>(configuration.GetSection("MercadoPago"));
        services.AddHttpClient<IMercadoPagoClient, MercadoPagoClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<MercadoPagoOptions>>().Value;
            client.BaseAddress = new Uri("https://api.mercadopago.com");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);
        });
        services.AddHttpClient<IExchangeRateService, DolarApiExchangeRateService>(client =>
        {
            client.BaseAddress = new Uri("https://dolarapi.com/");
        });

        return services;
    }
}
