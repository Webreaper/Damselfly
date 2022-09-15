using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Damselfly.Core.DbModels.Authentication;
using Damselfly.Core.Utils;
using Damselfly.Shared.Utils;
using EFCore.BulkExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.DBAbstractions;

/// <summary>
///     Base model for DB interactions. Will be passed a model class instance with an
///     implmentation of a DB type - e.g., SqlIe or MySql, kindof like a visitor
///     pattern.
/// </summary>
public abstract class BaseDBModel : IdentityDbContext<AppIdentityUser, ApplicationRole, int,
    IdentityUserClaim<int>, ApplicationUserRole, IdentityUserLogin<int>,
    IdentityRoleClaim<int>, IdentityUserToken<int>>
{
    public BaseDBModel(DbContextOptions options) : base(options)
    {
    }

    public static readonly ILoggerFactory SqlLoggerFactory
        = LoggerFactory.Create(builder => { builder.AddConsole(); });

#if DEBUG
    private static readonly bool traceSQL = false;
#else
        static bool traceSQL = false;
#endif

    private static readonly bool lazyLoad = false;

    public static bool ReadOnly { get; set; }

    /// <summary>
    ///     Basic initialisation for the DB that are generic to all DB types
    /// </summary>
    /// <param name="options"></param>
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if ( traceSQL )
            options.UseLoggerFactory(SqlLoggerFactory);

        if ( lazyLoad )
            options.UseLazyLoadingProxies();

        // Default to no tracking for performance. We can use Attach or 
        // AsTracking explicitly for when we need to do write operations.
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
    }

    /// <summary>
    ///     Bulk insert weapper for the database specialisation type.
    /// </summary>
    /// <typeparam name="T">T   ype of the object to insert</typeparam>
    /// <param name="db">DB model</param>
    /// <param name="collection">DbSet into which we're inserting the objects</param>
    /// <param name="itemsToSave">Objects to insert</param>
    /// <returns>True if the insert succeeded</returns>
    public async Task<bool> BulkInsert<T>(DbSet<T> collection, List<T> itemsToSave) where T : class
    {
        if ( ReadOnly )
        {
            Logging.LogVerbose("Read-only mode - no data will be inserted.");
            return true;
        }

        var success = false;
        try
        {
            var bulkConfig = new BulkConfig { SetOutputIdentity = true, BatchSize = 500 };

            await this.BulkInsertAsync(itemsToSave, bulkConfig);

            success = true;
        }
        catch ( Exception ex )
        {
            Logging.LogError($"Exception during bulk insert: {ex}");
            throw;
        }

        return success;
    }

    /// <summary>
    ///     Bulk update weapper for the database specialisation type.
    /// </summary>
    /// <typeparam name="T">Type of the object to update</typeparam>
    /// <param name="db">DB model</param>
    /// <param name="collection">DbSet into which we're updating the objects</param>
    /// <param name="itemsToSave">Objects to update</param>
    /// <returns>True if the update succeeded</returns>
    public async Task<bool> BulkUpdate<T>(DbSet<T> collection, List<T> itemsToSave) where T : class
    {
        // TODO make this method protected and then move this check to the base class
        if ( ReadOnly )
        {
            Logging.LogVerbose("Read-only mode - no data will be updated.");
            return true;
        }

        var success = false;
        try
        {
            collection.UpdateRange(itemsToSave);
            await SaveChangesAsync();

            // TODO: Replace when EFCore 7 has this
            //await db.BulkUpdateAsync(itemsToSave);

            success = true;
        }
        catch ( Exception ex )
        {
            Logging.LogError($"Exception during bulk update: {ex}");
            throw;
        }

        return success;
    }

    /// <summary>
    ///     Bulk insert weapper for the database specialisation type.
    /// </summary>
    /// <typeparam name="T">Type of the object to insert</typeparam>
    /// <param name="db">DB model</param>
    /// <param name="collection">DbSet into which we're inserting the objects</param>
    /// <param name="itemsToDelete">Objects to insert</param>
    /// <returns>True if the insert succeeded</returns>
    public async Task<bool> BulkDelete<T>(DbSet<T> collection, List<T> itemsToDelete) where T : class
    {
        if ( ReadOnly )
        {
            Logging.LogVerbose("Read-only mode - no data will be deleted.");
            return true;
        }

        var success = false;
        try
        {
            collection.RemoveRange(itemsToDelete);
            await SaveChangesAsync();

            // TODO: Replace when EFCore 7 has this
            // await db.BulkDeleteAsync(itemsToDelete);

            success = true;
        }
        catch ( Exception ex )
        {
            Logging.LogError($"Exception during bulk delete: {ex}");
            throw;
        }

        return success;
    }

    /// <summary>
    ///     Wrapper to extract the underlying BatchDelete implementation depending on the
    ///     DB model being used.
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public async Task<int> BatchDelete<T>(IQueryable<T> query) where T : class
    {
        if ( ReadOnly )
            return 1;

        return await query.ExecuteDeleteAsync();
    }

    /// <summary>
    ///     Wrapper to extract the underlying BatchDelete implementation depending on the
    ///     DB model being used.
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public async Task<int> BatchUpdate<T>(IQueryable<T> query,
        Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> updateExpression) where T : class
    {
        if ( ReadOnly )
            return 1;

        try
        {
            return await query.ExecuteUpdateAsync(updateExpression);
        }
        catch ( Exception ex )
        {
            Logging.LogError($"Exception during batch update: {ex.Message}");
            return 0;
        }
    }

    protected void ExecutePragma(string pragmaCommand)
    {
        try
        {
            var connection = Database.GetDbConnection();
            connection.Open();
            using ( var command = connection.CreateCommand() )
            {
                command.CommandText = pragmaCommand;
                command.ExecuteNonQuery();
            }
        }
        catch ( Exception ex )
        {
            Logging.LogWarning($"Unable to execute pragma command {pragmaCommand}: {ex.Message}");
        }
    }

    /// <summary>
    ///     Called whenever we Save Changes, this is an EF helper that interrogates
    ///     the change tracker and dumps a log of all of the changes in it. So we'll
    ///     see something like:
    ///     Insert Image: 5
    ///     Update Lens: 3
    ///     Updae Folder: 6
    /// </summary>
    private void LogChangeSummary()
    {
        if ( !Logging.Trace )
            return;

        var allChanges = ChangeTracker.Entries()
            .GroupBy(x => x.State + " " + x.Entity.GetType())
            .Select(x => new { Change = x.Key, Number = x.Count() })
            .OrderBy(x => x.Change)
            .ToList();

        Logging.LogTrace("Changes Summary:");
        if ( allChanges.Any() )
            foreach ( var x in allChanges )
                Logging.LogTrace("  {0} {1}", x.Change, x.Number);
        else
            Logging.LogTrace("  No changes.");
    }

    /// <summary>
    ///     Helper to clear any changes in the tracker (used if we're manually
    ///     writing them, or want to reset state mid-scope, or we want to throw
    ///     away changes unilaterally (e.g., if SaveChanges is called in ReadOnly
    ///     mode.
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
    ///     Log all writes to the DB - including the set of changes
    ///     written and whether we succeeded.
    /// </summary>
    /// <param name="contextDesc"></param>
    /// <returns>The number of entities written to the DB</returns>
    public async Task<int> SaveChangesAsync(string contextDesc,
        [CallerFilePath] string sourceFilePath = "",
        [CallerMemberName] string sourceMethod = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if ( ReadOnly )
        {
            Logging.LogVerbose("Read-only mode - no data will be updated.");
            ClearChangeTracker();
            return 1;
        }

        var retriesRemaining = 3;
        var recordsWritten = 0;

        while ( retriesRemaining > 0 )
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
            catch ( Exception ex )
            {
                if ( ex.Message.Contains("database is locked") && retriesRemaining > 0 )
                {
                    Logging.LogWarning(
                        $"Database locked for {contextDesc} - sleeping for 5s and retying {retriesRemaining}...");
                    retriesRemaining--;
                    await Task.Delay(5 * 1000);
                }
                else
                {
                    Logging.LogError($"Exception - DB WRITE FAILED for {contextDesc}: {ex.Message}");
                    Logging.LogError($"  Called from {sourceMethod} line {lineNumber} in {sourceFilePath}.");
                    if ( ex.InnerException != null )
                        Logging.LogError("  Exception - DB WRITE FAILED. InnerException: {0}",
                            ex.InnerException.Message);

                    // No retries if it's not a locked DB
                    break;
                }
            }

        return recordsWritten;
    }

    /// <summary>
    ///     Sync version of SaveChanges
    /// </summary>
    /// <param name="contextDesc"></param>
    /// <returns></returns>
    public int SaveChanges(string contextDesc)
    {
        return SaveChangesAsync(contextDesc).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     SQLite pragma execution.
    /// </summary>
    /// <param name="db"></param>
    /// <param name="pragmaCommand"></param>
    private static void ExecutePragma(BaseDBModel db, string pragmaCommand)
    {
        try
        {
            var connection = db.Database.GetDbConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = pragmaCommand;
            command.ExecuteNonQuery();
        }
        catch ( Exception ex )
        {
            Logging.LogWarning($"Unable to execute pragma command {pragmaCommand}: {ex.Message}");
        }
    }

    /// <summary>
    ///     Enable SQLite performance improvements
    /// </summary>
    /// <param name="db"></param>
    public void IncreasePerformance()
    {
        // Increase the timeout from the default (which I think is 30s)
        // To help concurrency.
        Database.SetCommandTimeout(60);
        // Enable journal mode - this will also improve
        // concurrent acces
        ExecutePragma(this, "PRAGMA journal_mode=WAL;");
        // Turn off Synchronous mode. This means that writes aren't
        // sync'd to disk every single time. 
        ExecutePragma(this, "PRAGMA synchronous=OFF;");
        // Increate the cache page size TODO: check this value
        ExecutePragma(this, "PRAGMA cache_size=10000;");
        // Use a shared cache - good for multi-threaded access
        ExecutePragma(this, "PRAGMA cache=shared;");
        // Allow reading from the cache. Means we might get stale
        // data, but in most cases that's fine and concurrency will
        // be improved. 
        ExecutePragma(this, "PRAGMA read_uncommitted=true;");
        // Store temporary tables in memory
        ExecutePragma(this, "PRAGMA temp_store=MEMORY;");

        // Massive hack....
        Logging.LogTrace("Deleting corrupt ImageMetaData entries");
        Database.ExecuteSqlRaw("delete from imagemetadata where Lastupdated = 1998;");

        OptimiseDB();

        RebuildFreeText().Wait();
    }

    private void OptimiseDB()
    {
        Logging.Log("Running Sqlite DB optimisation...");
        Database.ExecuteSqlRaw("VACUUM;");
        Logging.Log("DB optimisation complete.");
    }

    /// <summary>
    ///     If the DB supports it, and write-caching is enabled, flush.
    /// </summary>
    public void FlushDBWriteCache()
    {
        ExecutePragma(this, "PRAGMA schema.wal_checkpoint;");
    }

    // Can this be made async?
    public Task<IQueryable<T>> ImageSearch<T>(DbSet<T> resultSet, string query, bool includeAITags) where T : class
    {
        // Convert the string from a set of terms to quote and add * so they're all exact partial matches
        // TODO: How do we handle suffix matches - i.e., contains. SQLite FTS doesn't support that. :(
        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var sql = "SELECT i.* from Images i";
        var i = 1;
        var parms = new List<string>();

        /// Some hoop-jumping to escape the text terms, and then put them into parameters so we can pass to ExecuteSqlRaw
        /// without risk of SQL injection. Unfortunately, though, it appears that MATCH doesn't support @param type params
        /// and gives a syntax error. So it doesn't seem there's any way around doing this right now. We'll mitigate by
        /// stripping out semi-colons etc from the search term.
        foreach ( var term in terms.Select(x => Sanitize(x)) )
        {
            parms.Add(term);

            var ftsTerm = $"\"{term}\"*";
            var termParam = $"{{{i - 1}}}"; // Param like {0}
            var tagSubQuery =
                $"select distinct it.ImageId from FTSKeywords ftsKw join ImageTags it on it.tagId = ftsKw.TagId where ftsKw.Keyword MATCH ('{ftsTerm}')";
            var joinSubQuery = tagSubQuery;

            if ( includeAITags )
            {
                var objectSubQuery =
                    $"select distinct io.ImageId from FTSKeywords ftsObj join ImageObjects io on io.tagId = ftsObj.TagId where ftsObj.Keyword MATCH ('{ftsTerm}')";
                var nameSubQuery =
                    $"select distinct io.ImageId from FTSNames ftsName join ImageObjects io on io.PersonID = ftsName.PersonID where ftsName.Name MATCH ('{ftsTerm}')";
                joinSubQuery = $"{tagSubQuery} union {objectSubQuery} union {nameSubQuery}";
            }

            var imageSubQuery = "select distinct fts.ImageId from FTSImages fts where ";
            imageSubQuery += $"fts.Caption MATCH ('{ftsTerm}') OR ";
            // imageSubQuery += $"fts.Copyright MATCH ('{ftsTerm}') OR "; // Copyright search doesn't make that much sense
            // imageSubQuery += $"fts.Credit MATCH ('{ftsTerm}') OR ";
            imageSubQuery += $"fts.Description MATCH ('{ftsTerm}') ";
            joinSubQuery = $"{joinSubQuery} union {imageSubQuery}";

            // Subquery to produce the distinct set of images that match the term
            var subQuery = $" join ({joinSubQuery}) term{i} on term{i}.ImageID = i.ImageId";
            sql += subQuery;
            i++;
        }

        return Task.FromResult(resultSet.FromSqlRaw(sql, terms));
    }

    private async Task RebuildFreeText()
    {
        const string delete = @"DELETE from FTSKeywords; DELETE from FTSImages; DELETE from FTSNames;";
        const string insertTags = @"INSERT INTO FTSKeywords (TagId, Keyword) SELECT t.TagId, t.Keyword FROM Tags t;";
        const string insertPeople =
            @"INSERT INTO FTSNames (PersonID, Name) SELECT PersonId, Name FROM people p where p.State = 1;";
        const string insertImages = @"INSERT INTO FTSImages ( ImageId, Caption, Description, Copyright, Credit ) 
                                SELECT i.ImageId, i.Caption, i.Description, i.CopyRight, i.Credit FROM imagemetadata i 
                                WHERE (coalesce(i.Caption, '') <> '' OR coalesce(i.Description, '') <> '' 
                                     OR coalesce(i.Copyright, '') <> '' OR coalesce(i.Credit, '') <> '');";

        var sql = $"{delete} {insertTags} {insertPeople} {insertImages}";

        Logging.LogVerbose("Rebuilding Free Text Index.");
        await Database.ExecuteSqlRawAsync(sql);
        Logging.Log("Full-text search index rebuilt.");
    }

    private static string Sanitize(string input)
    {
        return input.Replace(";", " ").Replace("--", " ").Replace("#", " ").Replace("\'", "").Replace("\"", "");
    }
}