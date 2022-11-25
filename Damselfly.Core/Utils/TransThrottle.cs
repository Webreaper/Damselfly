using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Models;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Rest;

namespace Damselfly.Core.Utils;

/// <summary>
///     A transaction throttle for Azure. We start a 1-minute window,
///     and then within that window we count transactions. If we get
///     to 20, wait until the end of the minute Window, then start
///     again.
/// </summary>
public class TransThrottle : ITransactionThrottle
{
    /// <summary>
    /// https://stackoverflow.com/questions/34315589/queue-of-async-tasks-with-throttling-which-supports-muti-threading
    /// </summary>
    public class TimeGatedSemaphore
    {
        private readonly SemaphoreSlim semaphore;

        public TimeGatedSemaphore(int maxRequest, TimeSpan minimumHoldTime)
        {
            semaphore = new SemaphoreSlim(maxRequest);
            MinimumHoldTime = minimumHoldTime;
        }

        public TimeSpan MinimumHoldTime { get; }

        public async Task<IDisposable> WaitAsync()
        {
            await semaphore.WaitAsync();
            return new InternalReleaser(semaphore, Task.Delay(MinimumHoldTime));
        }

        private class InternalReleaser : IDisposable
        {
            private readonly SemaphoreSlim semaphoreToRelease;
            private readonly Task notBeforeTask;

            public InternalReleaser(SemaphoreSlim semaphoreSlim, Task dependantTask)
            {
                semaphoreToRelease = semaphoreSlim;
                notBeforeTask = dependantTask;
            }

            public void Dispose()
            {
                notBeforeTask.ContinueWith(_ => semaphoreToRelease.Release());
            }
        }
    }

    private readonly MonthTransCount _monthTransCount;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CloudTransaction.TransactionType _serviceType = CloudTransaction.TransactionType.AzureFace;
    private int _maxTransPerMinute = 20;
    private int _maxTransPerMonth = 30000;
    private volatile int _totalTransactions;
    private TimeGatedSemaphore gatedSemaphore;

    public TransThrottle(IServiceScopeFactory factory)
    {
        SetLimits(30, 30000);

        var date = DateTime.UtcNow.Date;

        _scopeFactory = factory;

        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        var monthStart = new DateTime(date.Year, date.Month, 1, 0, 0, 0);
        var monthTrans = db.CloudTransactions.Where(x => x.Date >= monthStart && x.TransType == _serviceType)
            .Sum(x => x.TransCount);

        Logging.Log($"Monthly trans count initialised at {monthTrans} for {_serviceType}");

        _monthTransCount = new MonthTransCount { Year = date.Year, Month = date.Month, TransCount = monthTrans };
    }

    public int TotalTransactions => _totalTransactions;

    public string MonthlyTransactions =>
        $"{_monthTransCount.TransCount} ({_monthTransCount.Month}-{_monthTransCount.Month})";

    public void SetLimits(int maxTransPerMin, int maxTransPerMonth)
    {
        _maxTransPerMinute = maxTransPerMin;
        _maxTransPerMonth = maxTransPerMonth;

        gatedSemaphore = new TimeGatedSemaphore(maxTransPerMin, TimeSpan.FromMinutes(1));

        Logging.Log($"Transaction limits set to {_maxTransPerMinute}/min, and {_maxTransPerMonth}/month");
    }

    public bool Disabled => _monthTransCount.TransCount >= _maxTransPerMonth;

    /// <summary>
    ///     Wrapper for Face Service calls to manage throttling and retries
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="desc"></param>
    /// <param name="method"></param>
    /// <returns></returns>
    public async Task<T> Call<T>(string desc, Task<T> method)
    {
        var t = default( T );
        var retries = 3;

        while (retries-- > 0)
        {
            try
            {
                using (await gatedSemaphore.WaitAsync())
                {
                    t = await method;
                }

                retries = 0;
            }
            catch (ErrorException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.TooManyRequests && retries > 0)
                {
                    Logging.LogWarning($"Azure throttle error: {ex.Response.Content}. Retrying {retries} more times.");
                }
                else
                    throw;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("'faceIds' exceeds maximum item count of '10'"))
                {
                    Logging.LogWarning("Photo had more than 10 faces. This is not supported in the free Azure API.");
                    retries = 0; // Bail
                }
                else
                {
                    Logging.LogError($"Unexpected exception in TransThrottle: {ex}");
                    throw;
                }
            }
        }

        return t;
    }

    /// <summary>
    ///     Store an aggregated count of Cloud Transactions
    /// </summary>
    public void ProcessNewTransactions()
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        var type = CloudTransaction.TransactionType.AzureFace;

        var today = DateTime.UtcNow.Date;

        var count = _totalTransactions;
        _totalTransactions = 0;

        if ( count > 0 )
        {
            if ( today.Year != _monthTransCount.Year || today.Month != _monthTransCount.Month )
            {
                _monthTransCount.Year = today.Year;
                _monthTransCount.Month = today.Month;
                _monthTransCount.TransCount = 0;
            }

            _monthTransCount.TransCount += _totalTransactions;

            var monthStart = new DateTime(2021, today.Month, 1, 0, 0, 1);
            var todayTrans = db.CloudTransactions.Where(x => x.Date == monthStart && x.TransType == _serviceType)
                .FirstOrDefault();

            if ( todayTrans == null )
            {
                todayTrans = new CloudTransaction { Date = today, TransType = type, TransCount = count };
                db.CloudTransactions.Add(todayTrans);
            }
            else
            {
                todayTrans.TransCount += count;
                db.CloudTransactions.Update(todayTrans);
            }

            db.SaveChanges("TransCount");
        }
    }

   
    private class MonthTransCount
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int TransCount { get; set; }
    }
}