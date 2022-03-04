using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Damselfly.Core.DbModels.Interfaces;
using Damselfly.Core.Utils;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace Damselfly.Core.DbModels.DBAbstractions
{
    /// <summary>
    /// Base model for DB interactions. Will be passed a model class instance with an
    /// implmentation of a DB type - e.g., SqlIe or MySql, kindof like a visitor
    /// pattern.
    ///
    /// 
    /// </summary>

    
    public abstract class BaseDBModel : IdentityDbContext<AppIdentityUser, ApplicationRole, int,
                                        IdentityUserClaim<int>, ApplicationUserRole, IdentityUserLogin<int>,
                                        IdentityRoleClaim<int>, IdentityUserToken<int>>
    {
        public static readonly ILoggerFactory SqlLoggerFactory
            = LoggerFactory.Create(builder => { builder.AddConsole(); });

#if DEBUG
        static bool traceSQL = false;
#else
        static bool traceSQL = false;
#endif

        static bool lazyLoad = false;

        public static bool ReadOnly { get; private set; }

        // Instance of our DB type that implements the Database interface
        public static IDataBase DatabaseSpecialisation { get; set; } 

        public void AddSpecialisationIndexes( ModelBuilder modelBuilder )
        {
            DatabaseSpecialisation.CreateIndexes(modelBuilder);
        }

        /// <summary>
        /// Basic initialisation for the DB that are generic to all DB types
        /// </summary>
        /// <param name="options"></param>
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (traceSQL)
                options.UseLoggerFactory(SqlLoggerFactory);

            if (lazyLoad)
                options.UseLazyLoadingProxies();

            // Default to no tracking for performance. We can use Attach or 
            // AsTracking explicitly for when we need to do write operations.
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);

            // See efmigrations.md
            //var obj = Activator.CreateInstance("Damselfly.Migrations.Sqlite", "Damselfly.Migrations.Sqlite.Models.SqlLiteModel");
            //var obj = Activator.CreateInstance("Damselfly.Migrations.Postgres", "Damselfly.Migrations.Postgres.Models.PostgresModel");

            DatabaseSpecialisation.Configure(options);
        }

        /// <summary>
        /// Bulk insert weapper for the database specialisation type. 
        /// </summary>
        /// <typeparam name="T">T   ype of the object to insert</typeparam>
        /// <param name="db">DB model</param>
        /// <param name="collection">DbSet into which we're inserting the objects</param>
        /// <param name="itemsToSave">Objects to insert</param>
        /// <returns>True if the insert succeeded</returns>
        public async Task<bool> BulkInsert<T>(DbSet<T> collection, List<T> itemsToSave) where T : class
        {
            if (ReadOnly)
                return true;

            return await DatabaseSpecialisation.BulkInsert(this, collection, itemsToSave);
        }

        /// <summary>
        /// Bulk update weapper for the database specialisation type. 
        /// </summary>
        /// <typeparam name="T">Type of the object to update</typeparam>
        /// <param name="db">DB model</param>
        /// <param name="collection">DbSet into which we're updating the objects</param>
        /// <param name="itemsToSave">Objects to update</param>
        /// <returns>True if the update succeeded</returns>
        public async Task<bool> BulkUpdate<T>(DbSet<T> collection, List<T> itemsToSave) where T : class
        {
            if (ReadOnly)
                return true;

            return await DatabaseSpecialisation.BulkUpdate(this, collection, itemsToSave);
        }

        /// <summary>
        /// Bulk insert weapper for the database specialisation type. 
        /// </summary>
        /// <typeparam name="T">Type of the object to insert</typeparam>
        /// <param name="db">DB model</param>
        /// <param name="collection">DbSet into which we're inserting the objects</param>
        /// <param name="itemsToDelete">Objects to insert</param>
        /// <returns>True if the insert succeeded</returns>
        public async Task<bool> BulkDelete<T>(DbSet<T> collection, List<T> itemsToDelete) where T : class
        {
            if (ReadOnly)
                return true;

            try
            {
                return await DatabaseSpecialisation.BulkDelete(this, collection, itemsToDelete);
            }
            catch( Exception ex )
            {
                Logging.LogError($"Exception during batch delete: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Wrapper to extract the underlying BatchDelete implementation depending on the
        /// DB model being used.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public async Task<int> BatchDelete<T>(IQueryable<T> query) where T : class
        {
            if (ReadOnly)
                return 1;

            try
            {
                return await DatabaseSpecialisation.BatchDelete(query);
            }
            catch(Exception ex )
            {
                Logging.LogError($"Exception during batch delete: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Wrapper to extract the underlying BatchDelete implementation depending on the
        /// DB model being used.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public async Task<int> BatchUpdate<T>(IQueryable<T> query, Expression<Func<T, T>> updateExpression) where T : class
        {
            if (ReadOnly)
                return 1;

            try
            { 
                return await DatabaseSpecialisation.BatchUpdate(query, updateExpression);
            }
            catch( Exception ex )
            {
                Logging.LogError($"Exception during batch update: {ex.Message}");
                return 0;
            }
        }


        /// <summary>
        /// Bulk insert weapper for the database specialisation type. 
        /// </summary>
        /// <typeparam name="T">Type of the object to insert</typeparam>
        /// <param name="db">DB model</param>
        /// <param name="collection">DbSet into which we're inserting the objects</param>
        /// <param name="itemsToDelete">Objects to insert</param>
        /// <returns>True if the insert succeeded</returns>
        /// Note: Currently unused, hence private.
        private async Task<bool> BulkInsertOrUpdate<T>(DbSet<T> collection, List<T> itemsToSave, Func<T, bool> isNew) where T : class
        {
            if (ReadOnly)
                return true;

            return await DatabaseSpecialisation.BulkInsertOrUpdate(this, collection, itemsToSave, isNew);
        }

        /// <summary>
        /// If the DB supports it, and write-caching is enabled, flush.
        /// </summary>
        public void FlushDBWriteCache()
        {
            DatabaseSpecialisation.FlushDBWriteCache( this );
        }

        /// <summary>
        /// Initialise the DB instance of the model type, using a DB specialisation. 
        /// </summary>
        /// <typeparam name="ModelType"></typeparam>
        /// <param name="dbSpecialisation">Instance of our DB specialisation</param>
        /// <param name="readOnly"></param>
        public static void InitDB<ModelType>( IDataBase dbSpecialisation, bool readOnly ) where ModelType : BaseDBModel
        {
            DatabaseSpecialisation = dbSpecialisation;
            ReadOnly = readOnly;

            using (var db = Activator.CreateInstance<ModelType>())
            {
                dbSpecialisation.Init( db );
            }
        }

        protected void ExecutePragma(string pragmaCommand)
        {
            try
            {
                var connection = Database.GetDbConnection();
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
        /// Called whenever we Save Changes, this is an EF helper that interrogates
        /// the change tracker and dumps a log of all of the changes in it. So we'll
        /// see something like:
        ///    Insert Image: 5
        ///    Update Lens: 3
        ///    Updae Folder: 6
        /// </summary>
        private void LogChangeSummary()
        {
            if (!Logging.Trace)
                return;

            var allChanges = ChangeTracker.Entries()
                                      .GroupBy(x => x.State.ToString() + " " + x.Entity.GetType())
                                      .Select(x => new { Change = x.Key, Number = x.Count() })
                                      .OrderBy(x => x.Change)
                                      .ToList();

            Logging.LogTrace("Changes Summary:");
            if (allChanges.Any())
            {
                foreach (var x in allChanges)
                {
                    Logging.LogTrace("  {0} {1}", x.Change, x.Number);
                }
            }
            else
                Logging.LogTrace("  No changes.");
        }

        /// <summary>
        /// Helper to clear any changes in the tracker (used if we're manually
        /// writing them, or want to reset state mid-scope, or we want to throw
        /// away changes unilaterally (e.g., if SaveChanges is called in ReadOnly
        /// mode.
        /// </summary>
        private void ClearChangeTracker()
        {
            ChangeTracker.Entries()
                .Where(e => e.Entity != null).ToList()
                .ForEach(e => e.State = EntityState.Detached);
        }

        public override int SaveChanges()
        {
            return SaveChanges("Unknown");
        }

        /// <summary>
        /// Log all writes to the DB - including the set of changes
        /// written and whether we succeeded.
        /// </summary>
        /// <param name="contextDesc"></param>
        /// <returns>The number of entities written to the DB</returns>
        public async Task<int> SaveChangesAsync(string contextDesc,
                [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
                [System.Runtime.CompilerServices.CallerMemberNameAttribute] string sourceMethod = "",
                [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0 )
        {
            if (ReadOnly)
            {
                Logging.LogVerbose("Read-only mode - no data will be updated.");
                ClearChangeTracker();
                return 1;
            }

            int retriesRemaining = 3;
            int recordsWritten = 0;

            while ( retriesRemaining > 0 )
            {
                try
                {
                    // Write to the DB
                    var watch = new Stopwatch("SaveChanges" + contextDesc);

                    LogChangeSummary();

                    recordsWritten = await base.SaveChangesAsync();

                    Logging.LogTrace("{0} changes written to the DB", recordsWritten);

                    watch.Stop();

                    break;
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("database is locked") && retriesRemaining > 0 )
                    {
                        Logging.LogWarning($"Database locked for {contextDesc} - sleeping for 5s and retying {retriesRemaining}...");
                        retriesRemaining--;
                        await Task.Delay(5 * 1000);
                    }
                    else
                    {
                        Logging.LogError($"Exception - DB WRITE FAILED for {contextDesc}: {ex.Message}" );
                        Logging.LogError($"  Called from {sourceMethod} line {lineNumber} in {sourceFilePath}.");
                        if (ex.InnerException != null)
                            Logging.LogError("  Exception - DB WRITE FAILED. InnerException: {0}", ex.InnerException.Message);

                        // No retries if it's not a locked DB
                        break;
                    }
                }

            }

            return recordsWritten;
        }

        /// <summary>
        /// Sync version of SaveChanges
        /// </summary>
        /// <param name="contextDesc"></param>
        /// <returns></returns>
        public int SaveChanges(string contextDesc)
        {
            return SaveChangesAsync(contextDesc).GetAwaiter().GetResult();
        }

        // TODO - this is Sqlite specific and should move down into the MySqlite provider.
        public async Task<IQueryable<T>> ImageSearch<T>(DbSet<T> resultSet, string query, bool includeAITags) where T:class
        {
            return await Task.Run(() => DatabaseSpecialisation.ImageSearch(resultSet, query, includeAITags));
        }
    }
}