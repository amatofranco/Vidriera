using System.Linq.Expressions;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Common.Exceptions;

namespace Vidriera.Application.Common;

internal static class EntityLookup
{
    public static async Task<TEntity> GetOrThrowAsync<TEntity>(
        this IQueryable<TEntity> query,
        Expression<Func<TEntity, bool>> predicate,
        string notFoundMessage,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var entity = await query.FirstOrDefaultAsync(predicate, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(notFoundMessage);
        }
        return entity;
    }

    public static async Task<TEntity> GetOrThrowAsync<TEntity>(
        this ISession session,
        Guid id,
        string notFoundMessage,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var entity = await session.GetAsync<TEntity>(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(notFoundMessage);
        }
        return entity;
    }

    public static async Task SaveInTransactionAsync(this ISession session, object entity, CancellationToken cancellationToken)
    {
        using var transaction = session.BeginTransaction();
        await session.SaveAsync(entity, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public static async Task UpdateInTransactionAsync(this ISession session, object entity, CancellationToken cancellationToken)
    {
        using var transaction = session.BeginTransaction();
        await session.UpdateAsync(entity, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public static async Task DeleteInTransactionAsync(this ISession session, object entity, CancellationToken cancellationToken)
    {
        using var transaction = session.BeginTransaction();
        await session.DeleteAsync(entity, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
