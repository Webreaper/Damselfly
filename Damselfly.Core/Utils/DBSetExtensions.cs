using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Damselfly.Core.DBAbstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Damselfly.Core.Utils;

/// <summary>
/// Some helper extension methods for DbSet operations
/// </summary>
public static class DbSetExtensions
{
    /// <summary>
    /// Useful little helper to add an item but only if it's not already in the set,
    /// according to the predicate passed.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="set"></param>
    /// <param name="predicate"></param>
    /// <param name="entity"></param>
    /// <returns></returns>
    public static EntityEntry<T> AddIfNotExists<T>(this DbSet<T> set, Expression<Func<T, bool>> predicate, T entity) where T : class, new()
    {
        return !set.Any(predicate) ? set.Add(entity) : null;
    }

    /// <summary>
    /// Hacked bulk update that uses SaveChanges under the cover - useful for preview
    /// releases where EFCore.BulkExtensions is broken. Hopefully redundant when EFCore
    /// supports bulk updates natively.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="db"></param>
    /// <param name="query"></param>
    /// <param name="updateExpression"></param>
    /// <returns></returns>
    public static async Task<int> BulkUpdateWithSaveChanges<T>(this BaseDBModel db, IQueryable<T> query, Expression<Func<T, T>> updateExpression) where T : class
    {


        var entities = await query.ToListAsync();
        var compiledExpression = updateExpression.Compile();
        var dbSet = db.Set<T>();
        List<string> memberNames = new List<string>();

        if (updateExpression.Body is MemberInitExpression memberInitExpression)
        {
            foreach (var item in memberInitExpression.Bindings)
            {
                if (item is MemberAssignment assignment)
                {
                    memberNames.Add(item.Member.Name);
                }
            }
        }

        foreach (var e in entities)
        {
            var updated = compiledExpression.Invoke(e);

            updated.CopyPropertiesTo(e, memberNames);

            dbSet.Update(e);
        }

        return await db.SaveChangesAsync();
    }
}
