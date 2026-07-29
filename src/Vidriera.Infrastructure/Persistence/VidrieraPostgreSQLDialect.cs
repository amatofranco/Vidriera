using System.Data;

namespace Vidriera.Infrastructure.Persistence;

public class VidrieraPostgreSQLDialect : NHibernate.Dialect.PostgreSQLDialect
{
    public VidrieraPostgreSQLDialect()
    {
        RegisterColumnType(DbType.Guid, "uuid");
    }
}
