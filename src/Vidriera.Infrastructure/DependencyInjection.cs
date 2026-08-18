using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NHibernate;
using Vidriera.Application.Abstractions;
using Vidriera.Infrastructure.Persistence;
using Vidriera.Infrastructure.Storage;
using Vidriera.Infrastructure.Pdf;
using Vidriera.Infrastructure.Auth;
using Vidriera.Infrastructure.Excel;

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

        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        return services;
    }
}
