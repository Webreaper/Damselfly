using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using Damselfly.Core.Models.DBAbstractions;

namespace Damselfly.Core.Models.Interfaces
{
    /// <summary>
    /// Interface representing a type of DB (e.g., Sqlite, MySql or SQL Server.
    /// It provides a common interface to all DB types, for the operations we need.
    /// </summary>
    public interface IDataBase
    {
        void Init(BaseDBModel db);
        void Configure(DbContextOptionsBuilder options);
        void FlushDBWriteCache(BaseDBModel db);

        bool BulkUpdate<T>(BaseDBModel db, DbSet<T> collection, List<T> itemsToSave) where T : class;
        bool BulkInsert<T>(BaseDBModel db, DbSet<T> collection, List<T> itemsToSave) where T : class;
        bool BulkDelete<T>(BaseDBModel db, DbSet<T> collection, List<T> itemsToDelete) where T : class;
        bool BulkInsertOrUpdate<T>(BaseDBModel db, DbSet<T> collection, List<T> itemsToSave, Func<T, bool> isNew ) where T : class;

        IQueryable<T> ImageSearch<T>(DbSet<T> resultSet, string query) where T : class;
        void FullTextTags(bool first);
        void CreateIndexes(ModelBuilder builder);
    }
}
