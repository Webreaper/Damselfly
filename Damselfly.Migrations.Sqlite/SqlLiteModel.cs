using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Damselfly.Core.Models;
using Damselfly.Core.DbModels.Interfaces;
using Damselfly.Core.DbModels.DBAbstractions;
using System.Linq.Expressions;
using Damselfly.Core.Utils;
using SqlParameter = Microsoft.Data.Sqlite.SqliteParameter;
using EFCore.BulkExtensions;

namespace Damselfly.Migrations.Sqlite.Models
{
    /// <summary>
    /// SQLite database specialisation. Assumes a Database path is set
    /// at construction.
    /// </summary>
    public class SqlLiteModel : IDataBase
    {
        private string DatabasePath { get; set; }

        public SqlLiteModel()
        {
            Console.WriteLine("Constructing Sqlite Model for EFCore Migrations...");
            BaseDBModel.DatabaseSpecialisation = this;
        }

        public SqlLiteModel( string dbPath )
        {
            DatabasePath = dbPath;
        }

        /// <summary>
        /// The SQLite-specific initialisation.
        /// </summary>
        /// <param name="options"></param>
        public void Configure(DbContextOptionsBuilder options)
        {
            string dataSource = $"Data Source={DatabasePath}";
            options.UseSqlite(dataSource, b => b.MigrationsAssembly("Damselfly.Migrations.Sqlite"));
        }

        /// <summary>
        /// SQLite pragma execution.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="pragmaCommand"></param>
        private void ExecutePragma(BaseDBModel db, string pragmaCommand)
        {
            try
            {
                var connection = db.Database.GetDbConnection();
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = pragmaCommand;
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Logging.LogWarning($"Unable to execute pragma command {pragmaCommand}: {ex.Message}");
            }
        }

        /// <summary>
        /// Enable SQLite performance improvements
        /// </summary>
        /// <param name="db"></param>
        private void IncreasePerformance(BaseDBModel db)
        {
            // Increase the timeout from the default (which I think is 30s)
            // To help concurrency.
            db.Database.SetCommandTimeout(60);
            // Enable journal mode - this will also improve
            // concurrent acces
            ExecutePragma(db, "PRAGMA journal_mode=WAL;");
            // Turn off Synchronous mode. This means that writes aren't
            // sync'd to disk every single time. 
            ExecutePragma(db, "PRAGMA synchronous=OFF;");
            // Increate the cache page size TODO: check this value
            ExecutePragma(db, "PRAGMA cache_size=10000;");
            // Use a shared cache - good for multi-threaded access
            ExecutePragma(db, "PRAGMA cache=shared;");
            // Allow reading from the cache. Means we might get stale
            // data, but in most cases that's fine and concurrency will
            // be improved. 
            ExecutePragma(db, "PRAGMA read_uncommitted=true;");
            // Store temporary tables in memory
            ExecutePragma(db, "PRAGMA temp_store=MEMORY;");

            // Massive hack....
            Logging.LogTrace("Deleting corrupt ImageMetaData entries");
            db.Database.ExecuteSqlRaw("delete from imagemetadata where Lastupdated = 1998;");

            Logging.Log("Running Sqlite DB optimisation...");
            db.Database.ExecuteSqlRaw("VACUUM;");
            Logging.Log("DB optimisation complete.");

            RebuildFreeText(db);
        }

        public void FlushDBWriteCache(BaseDBModel db)
        {
            ExecutePragma(db, "PRAGMA schema.wal_checkpoint;");
        }

        /// <summary>
        /// SQLite specific initialisation. Run the migrations, and 
        /// always run a VACUUM to optimise the DB at startup.
        /// </summary>
        /// <param name="db"></param>
        public void Init( BaseDBModel db )
        {
            try
            {
                Logging.Log("Running Sqlite DB migrations...");
                db.Database.Migrate();
            }
            catch( Exception ex )
            {
                Logging.LogWarning($"Migrations failed with exception: {ex}");

                if( ex.InnerException != null )
                    Logging.LogWarning($"InnerException: {ex.InnerException}");

                Logging.Log($"Creating DB.");
                db.Database.EnsureCreated();
            }

            IncreasePerformance(db);
        }

        /// <summary>
        /// SQLite bulk insert uses EF Extensions BulkIndex.
        /// This would also work for SQLServer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="itemsToSave"></param>
        /// <returns></returns>
        public async Task<bool> BulkInsert<T>(BaseDBModel db, DbSet <T> collection, List<T> itemsToSave ) where T : class
        {
            if (BaseDBModel.ReadOnly)
            {
                Logging.LogVerbose("Read-only mode - no data will be inserted.");
                return true;
            }

            bool success = false;
            try
            {
                var bulkConfig = new BulkConfig { SetOutputIdentity = true, BatchSize = 500 };

                await db.BulkInsertAsync(itemsToSave, bulkConfig);

                success = true;
            }
            catch (Exception ex)
            {
                Logging.LogError($"Exception during bulk insert: {ex}");
            }

            return success;
        }

        /// <summary>
        /// SQLite bulk update uses EF Extensions BulkUpdate.
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

            bool success = false;
            try
            {
                //collection.UpdateRange(itemsToSave);
                //await db.SaveChangesAsync();

                await db.BulkUpdateAsync(itemsToSave);

                success = true;
            }
            catch (Exception ex)
            {
                Logging.LogError($"Exception during bulk update: {ex}");
            }

            return success;
        }

        /// <summary>
        /// SQLite bulk delete uses EF Extensions BulkDelete.
        /// This would also work for SQLServer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="itemsToDelete"></param>
        /// <returns></returns>
        public async Task<bool> BulkDelete<T>(BaseDBModel db, DbSet<T> collection, List<T> itemsToDelete) where T : class
        {
            if (BaseDBModel.ReadOnly)
            {
                Logging.LogVerbose("Read-only mode - no data will be deleted.");
                return true;
            }

            bool success = false;
            try
            {
                //collection.RemoveRange(itemsToDelete);
                //await db.SaveChangesAsync();

                await db.BulkDeleteAsync(itemsToDelete);

                success = true;
            }
            catch (Exception ex)
            {
                Logging.LogError($"Exception during bulk delete: {ex}");
            }

            return success;
        }

        /// <summary>
        /// Use the EF BulkExtensions to implement this.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="db"></param>
        /// <param name="collection"></param>
        /// <param name="itemsToSave"></param>
        /// <param name="getKey"></param>
        /// <returns></returns>
        public Task<bool> BulkInsertOrUpdate<T>(BaseDBModel db, DbSet<T> collection, List<T> itemsToSave, Func<T, bool> getKey) where T : class
        {
            throw new NotImplementedException();
        }

        public async Task<int> BatchDelete<T>(IQueryable<T> query) where T : class
        {
            // TODO Try/Catch here?
            return await query.BatchDeleteAsync();
        }

        public async Task<int> BatchUpdate<T>(IQueryable<T> query, Expression<Func<T,T>> updateExpression) where T : class
        {
            // TODO Try/Catch here?
            return await query.BatchUpdateAsync( updateExpression );
        }

        private string Sanitize( string input )
        {
            return input.Replace(";", " ").Replace( "--", " ").Replace( "#", " ").Replace( "\'", "" ).Replace( "\"", "");
        }

        public IQueryable<T> ImageSearch<T>(DbSet<T> resultSet, string query, bool includeAITags) where T : class
        {
            // Convert the string from a set of terms to quote and add * so they're all exact partial matches
            // TODO: How do we handle suffix matches - i.e., contains. SQLite FTS doesn't support that. :(
            var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var sql = "SELECT i.* from Images i";
            int i = 1;
            var parms = new List<string>();

            /// Some hoop-jumping to escape the text terms, and then put them into parameters so we can pass to ExecuteSqlRaw
            /// without risk of SQL injection. Unfortunately, though, it appears that MATCH doesn't support @param type params
            /// and gives a syntax error. So it doesn't seem there's any way around doing this right now. We'll mitigate by
            /// stripping out semi-colons etc from the search term.
            foreach ( var term in terms.Select( x => Sanitize( x) ) )
            {
                parms.Add(term);

                var ftsTerm = $"\"{term}\"*"; 
                var termParam = $"{{{i-1}}}"; // Param like {0}
                var tagSubQuery = $"select distinct it.ImageId from FTSKeywords ftsKw join ImageTags it on it.tagId = ftsKw.TagId where ftsKw.Keyword MATCH ('{ftsTerm}')";
                var joinSubQuery = tagSubQuery;

                if (includeAITags)
                {
                    var objectSubQuery = $"select distinct io.ImageId from FTSKeywords ftsObj join ImageObjects io on io.tagId = ftsObj.TagId where ftsObj.Keyword MATCH ('{ftsTerm}')";
                    var nameSubQuery = $"select distinct io.ImageId from FTSNames ftsName join ImageObjects io on io.PersonID = ftsName.PersonID where ftsName.Name MATCH ('{ftsTerm}')";
                    joinSubQuery = $"{tagSubQuery} union {objectSubQuery} union {nameSubQuery}";
                }

                var imageSubQuery = $"select distinct fts.ImageId from FTSImages fts where ";
                imageSubQuery += $"fts.Caption MATCH ('{ftsTerm}') OR ";
                imageSubQuery += $"fts.Description MATCH ('{ftsTerm}') OR ";
                imageSubQuery += $"fts.Copyright MATCH ('{ftsTerm}') OR ";
                imageSubQuery += $"fts.Credit MATCH ('{ftsTerm}')";
                joinSubQuery = $"{joinSubQuery} union {imageSubQuery}";

                // Subquery to produce the distinct set of images that match the term
                var subQuery = $" join ({joinSubQuery}) term{i} on term{i}.ImageID = i.ImageId";
                sql += subQuery;
                i++;
            }

            return resultSet.FromSqlRaw(sql, terms);
        }

        public void CreateIndexes(ModelBuilder builder)
        {
            // Nothing to do here.
        }

        private void RebuildFreeText(BaseDBModel db)
        {
            const string delete = @"DELETE from FTSKeywords; DELETE from FTSImages; DELETE from FTSNames;";
            const string insertTags = @"INSERT INTO FTSKeywords (TagId, Keyword) SELECT t.TagId, t.Keyword FROM Tags t;";
            const string insertPeople = @"INSERT INTO FTSNames (PersonID, Name) SELECT PersonId, Name FROM people p where p.State = 1;";
            const string insertImages = @"INSERT INTO FTSImages ( ImageId, Caption, Description, Copyright, Credit ) 
                                SELECT i.ImageId, i.Caption, i.Description, i.CopyRight, i.Credit FROM imagemetadata i 
                                WHERE (coalesce(i.Caption, '') <> '' OR coalesce(i.Description, '') <> '' 
                                     OR coalesce(i.Copyright, '') <> '' OR coalesce(i.Credit, '') <> '');";

            string sql = $"{delete} {insertTags} {insertPeople} {insertImages}";

            Logging.LogVerbose("Rebuilding Free Text Index.");
            db.Database.ExecuteSqlRaw(sql);
            Logging.Log("Full-text search index rebuilt.");
        }
    }
}