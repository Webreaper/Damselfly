using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Damselfly.Core.DbModels.Interfaces;
using Damselfly.Core.DbModels.DBAbstractions;
using Damselfly.Core.Models;
using System.Text.Json;
using System.IO;
using Z.EntityFramework.Plus;
using Z.EntityFramework.Extensions;
using Damselfly.Core.Utils;
using System.Threading.Tasks;

namespace Damselfly.Migrations.Postgres.Models
{
    /// <summary>
    /// Postgres database specialisation. Assumes a Database path is set
    /// at construction.
    /// </summary>
    public class PostgresModel : IDataBase
    {
        private class DBSettings
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string Host { get; set; } = "localhost";
            public int Port { get; set; } = 5432;
            public string DatabaseName { get; set; } = "Damselfly";
        }

        private readonly DBSettings dBSettings;

        public static PostgresModel ReadSettings( string settingsPath )
        {
            try
            {
                var jsonString = File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<DBSettings>(jsonString);

                var model = new PostgresModel(settings);
                return model;
            }
            catch( Exception ex )
            {
                Logging.LogError($"Invalid settings for Postgres: {ex.Message}");
                return null;
            }
        }

        public PostgresModel()
        {
            dBSettings = new DBSettings();
            Console.WriteLine("Constructing Postgres Model for EFCore Migrations...");
            BaseDBModel.DatabaseSpecialisation = this;
        }

        private PostgresModel( DBSettings settings )
        {
            dBSettings = settings;
        }

        /// <summary>
        /// The Postgres-specific initialisation.
        /// </summary>
        /// <param name="options"></param>
        public void Configure(DbContextOptionsBuilder options)
        {
            string dataSource = $"User ID={dBSettings.Username};Password={dBSettings.Password};";
            dataSource += $"Host={dBSettings.Host};Port={dBSettings.Port};Database={dBSettings.DatabaseName};Pooling=true;";
            options.UseNpgsql(dataSource, b => b.MigrationsAssembly("Damselfly.Migrations.Postgres"));
        }

        /// <summary>
        /// Enable Postgres performance improvements
        /// </summary>
        /// <param name="db"></param>
        private void IncreasePerformance(BaseDBModel db)
        {
            // Nothing yet for postgres
        }

        public void FlushDBWriteCache(BaseDBModel db)
        {
            // 
        }

        /// <summary>
        /// Postgres specific initialisation. Run the migrations, and 
        /// always run a VACUUM to optimise the DB at startup.
        /// </summary>
        /// <param name="db"></param>
        public void Init( BaseDBModel db )
        {
            try
            {
                Logging.Log("Running Postgres DB migrations...");
                db.Database.Migrate();
            }
            catch (Exception ex)
            {
                Logging.LogWarning("Migrations failed - creating DB. Exception: {0}", ex.Message);

                try
                {
                    db.Database.EnsureCreated();
                }
                catch (Exception ex2)
                {
                    Logging.LogError("Database creation failed. Exception: {0}", ex2.Message);
                }
            }

            IncreasePerformance(db);
        }

        /// <summary>
        /// Postgres bulk insert uses EF Extensions BulkIndex.
        /// This would also work for SQLServer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="itemsToSave"></param>
        /// <returns></returns>
        public async Task<bool> BulkInsert<T>(BaseDBModel db, DbSet <T> collection, List<T> itemsToSave ) where T : class
        {
            // TODO make this method protected and then move this check to the base class
            if (BaseDBModel.ReadOnly)
            {
                Logging.LogVerbose("Read-only mode - no data will be updated.");
                return true;
            }

            collection.AddRange(itemsToSave);

            // TODO: Set output identity here.
            int ret = await db.SaveChangesAsync("BulkSave");

            return ret == itemsToSave.Count;
        }

        /// <summary>
        /// Postgres bulk update uses EF Extensions BulkUpdate.
        /// This would also work for SQLServer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="itemsToSave"></param>
        /// <returns></returns>
        public async Task<bool> BulkUpdate<T>(BaseDBModel db, DbSet<T> collection, List<T> itemsToSave) where T : class
        {
            // TODO make this method protected and then move this check to the base class
            if (BaseDBModel.ReadOnly)
            {
                Logging.LogVerbose("Read-only mode - no data will be updated.");
                return true;
            }

            collection.UpdateRange(itemsToSave);

            int ret = await db.SaveChangesAsync("BulkSave");

            return ret == itemsToSave.Count;
        }

        /// <summary>
        /// Postgres bulk delete uses EF Extensions BulkDelete.
        /// This would also work for SQLServer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="itemsToDelete"></param>
        /// <returns></returns>
        public async Task<bool> BulkDelete<T>(BaseDBModel db, DbSet<T> collection, List<T> itemsToDelete) where T : class
        {
            // TODO make this method protected and then move this check to the base class
            if (BaseDBModel.ReadOnly)
            {
                Logging.LogVerbose("Read-only mode - no data will be deleted.");
                return true;
            }

            collection.RemoveRange(itemsToDelete);

            int ret = await db.SaveChangesAsync("BulkSave");

            return ret == itemsToDelete.Count;
        }

        /// <summary>
        /// Basic implementation that inserts or updates based on whether the key/ID is zero or not.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="db"></param>
        /// <param name="collection"></param>
        /// <param name="itemsToSave"></param>
        /// <param name="getKey"></param>
        /// <returns></returns>
        public async Task<bool> BulkInsertOrUpdate<T>(BaseDBModel db, DbSet<T> collection, List<T> itemsToSave, Func<T, bool> isNew) where T : class
        {
            var result = false;

            itemsToSave.ForEach(x => { if (isNew(x)) collection.Add(x); else collection.Update(x); });

            if( await db.SaveChangesAsync("BulkInsertOrUpdate") > 0 )
                result = true;

            return result;
        }

        /// <summary>
        /// Wrapper for batch delete on an IQueryable
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public async Task<int> BatchDelete<T>(IQueryable<T> query) where T : class
        {
            return await query.DeleteAsync();
        }

        public async Task<int> BatchUpdate<T>(IQueryable<T> query, Expression<Func<T, T>> updateExpression) where T : class
        {
            return await query.UpdateAsync(updateExpression);
        }


        public Task<IQueryable<T>> Search<T>(string query, DbSet<T> collection) where T : class
        {
            // Figure out FTS in Postgres
            // TODO: Implement with a Like Query?
            throw new NotImplementedException();
        }

        public IQueryable<T> ImageSearch<T>(DbSet<T> resultSet, string query, bool includeAITags) where T : class
        {
            // Figure out FTS in postgres
            // TODO: Implement with a Like Query?
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create the model-specific indexes
        /// </summary>
        /// <param name="modelBuilder"></param>
        /// <returns></returns>
        public void CreateIndexes(ModelBuilder modelBuilder)
        {
            EntityFrameworkManager.IsCommunity = true;

            modelBuilder.Entity<Tag>()
                .HasIndex(b => new { b.Keyword })
                .IsTsVectorExpressionIndex("english");
       }
    }
}