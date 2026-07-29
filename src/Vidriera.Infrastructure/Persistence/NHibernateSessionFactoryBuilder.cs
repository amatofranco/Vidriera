using NHibernate;
using NHibernate.Cfg;
using NHibernate.Mapping.ByCode;

namespace Vidriera.Infrastructure.Persistence;

public static class NHibernateSessionFactoryBuilder
{
    public static ISessionFactory BuildSessionFactory(string connectionString)
    {
        var configuration = new Configuration();

        configuration.DataBaseIntegration(db =>
        {
            db.Driver<NHibernate.Driver.NpgsqlDriver>();
            db.Dialect<VidrieraPostgreSQLDialect>();
            db.ConnectionString = connectionString;
            db.KeywordsAutoImport = Hbm2DDLKeyWords.None;
        });

        var mapper = new ModelMapper();
        mapper.AddMappings(typeof(NHibernateSessionFactoryBuilder).Assembly.GetExportedTypes());
        configuration.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

        return configuration.BuildSessionFactory();
    }
}
