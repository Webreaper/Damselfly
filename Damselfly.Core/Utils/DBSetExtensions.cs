using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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
}
