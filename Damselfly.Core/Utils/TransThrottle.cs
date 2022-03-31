using System;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Models;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Rest;

namespace Damselfly.Core.Utils;

/// <summary>
/// A transaction throttle for Azure. We start a 1-minute window,
/// and then within that window we count transactions. If we get
/// to 20, wait until the end of the minute Window, then start
/// again.
/// </summary>
public class TransThrottle : ITransactionThrottle
{
    private class MonthTransCount
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int TransCount { get; set; }
    };

    private DateTime _windowStart = DateTime.MinValue;
    private volatile int _totalTransactions;
    private volatile int _windowTransactions;
    private int _maxTransPerMinute;
    private int _maxTransPerMonth;
    private CloudTransaction.TransactionType _serviceType;

    private readonly MonthTransCount _monthTransCount;
    
    public TransThrottle(CloudTransaction.TransactionType serviceType, int maxTransPerMin = 20, int maxTransPerMonth = 30000 )
    {
        _serviceType = serviceType;

        SetLimits(maxTransPerMin, maxTransPerMonth);

        var date = DateTime.UtcNow.Date;
        using var db = new ImageContext();
        var monthStart = new DateTime(date.Year, date.Month, 1, 0, 0, 0);
        var monthTrans = db.CloudTransactions.Where(x => x.Date >= monthStart && x.TransType == _serviceType)
                                               .Sum(x => x.TransCount);

        Logging.Log($"Monthly trans count initialised at {monthTrans} for {_serviceType}");

        _monthTransCount = new MonthTransCount { Year = date.Year, Month = date.Month, TransCount = monthTrans };
    }

    public void SetLimits(int maxTransPerMin, int maxTransPerMonth)
    {
        _maxTransPerMinute = maxTransPerMin;
        _maxTransPerMonth = maxTransPerMonth;

        Logging.Log($"Transaction limits set to {_maxTransPerMinute}/min, and {_maxTransPerMonth}/month");
    }

    public bool Disabled { get { return _monthTransCount.TransCount >= _maxTransPerMonth;  } }

    private async Task WaitAfterTransaction(string desc)
    {
        var now = DateTime.UtcNow;

        if (_windowStart == DateTime.MinValue)
            _windowStart = now;

        var windowSecs = (int)(now - _windowStart).TotalSeconds;

        if ( windowSecs > 60 )
        {
            // Completed a minute - so clean slate.
            _windowStart = now;
            _windowTransactions = 0;                
        }

        // Increment our transaction count
        _windowTransactions++;
        _totalTransactions++;

        if ( _windowTransactions >= _maxTransPerMinute )
        {
            // Too many transactions in this 1-min window. So sleep.
            var sleepTime = (60 - windowSecs) + 5;
            if (sleepTime > 10)
            {
                // Log for long delays.
                Logging.Log($"Sleeping for {sleepTime}s to avoid Azure transaction throttle on {desc} API call.");
            }
            await Task.Delay(sleepTime * 1000);
        }
    }

    /// <summary>
    /// Wrapper for Face Service calls to manage throttling and retries
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="desc"></param>
    /// <param name="method"></param>
    /// <returns></returns>
    public async Task<T> Call<T>(string desc, Task<T> method)
    {
        T t = default(T);
        int retries = 3;

        while (retries-- > 0)
        {
            try
            {
                t = await method;
                await WaitAfterTransaction(desc);
                retries = 0;
            }
            catch (Exception ex)
            {
                retries = await HandleThrottleException(ex, retries);
            }
        }

        return t;
    }

    /// <summary>
    /// Wrapper for Face Service calls to manage throttling and retries
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="desc"></param>
    /// <param name="method"></param>
    /// <returns></returns>
    public async Task Run(string desc, Task method)
    {
        int retries = 3;

        while (retries-- > 0)
        {
            try
            {
                await method;
                await WaitAfterTransaction(desc);
                retries = 0;
            }
            catch (Exception ex)
            {
                retries = await HandleThrottleException(ex, retries);
            }
        }
    }

    /// <summary>
    /// If the throttling didn't work for some edge-case reason, we may get a
    /// TooManyRequests exception. So sleep for 5s, and then retry.
    /// </summary>
    /// <param name="ex"></param>
    /// <param name="retriesRemaining"></param>
    /// <returns>Number of retries (depending on this error this may be altered)</returns>
    private async Task<int> HandleThrottleException( Exception ex, int retriesRemaining )
    {
        int retriesToReturn = retriesRemaining;

        const int requestsDelay = 30;

        if (ex is ErrorException)
        {
            var errorEx = ex as ErrorException;
            if (errorEx.Response.StatusCode == System.Net.HttpStatusCode.TooManyRequests && retriesRemaining > 0)
            {
                Logging.LogWarning($"Azure throttle error: {errorEx.Response.Content}. Window Transcount: {_windowTransactions}. Retrying {retriesRemaining} more times.");
                await Task.Delay(requestsDelay * 1000);
            }
        }
        else if( ex is APIErrorException )
        {
            var errorEx = ex as APIErrorException;
            if (errorEx.Response.StatusCode == System.Net.HttpStatusCode.TooManyRequests && retriesRemaining > 0)
            {
                Logging.LogWarning($"Azure throttle API error: {errorEx.Response.Content}. Window Transcount: {_windowTransactions}. Retrying {retriesRemaining} more times.");
                await Task.Delay(requestsDelay * 1000);
            }
        }
        else if (ex is ValidationException)
        {
            var errorEx = ex as ValidationException;

            if( errorEx.Message.Contains("'faceIds' exceeds maximum item count of '10'") )
            {
                Logging.LogWarning("Photo had more than 10 faces. This is not supported in the free Azure API.");
                // No point retrying. All bets are off for this pic.
                retriesToReturn = 0;
            }
        }
        else
            throw ex;

        return retriesToReturn;
    }

    public int TotalTransactions { get { return _totalTransactions; } }
    public string MonthlyTransactions { get { return $"{_monthTransCount.TransCount} ({_monthTransCount.Month}-{_monthTransCount.Month})"; } }

    /// <summary>
    /// Store an aggregated count of Cloud Transactions
    /// </summary>
    public void ProcessNewTransactions()
    {
        using var db = new ImageContext();
        var type = CloudTransaction.TransactionType.AzureFace;

        DateTime today = DateTime.UtcNow.Date;

        var count = _totalTransactions;
        _totalTransactions = 0;

        if (count > 0)
        {
            if (today.Year != _monthTransCount.Year || today.Month != _monthTransCount.Month)
            {
                _monthTransCount.Year = today.Year;
                _monthTransCount.Month = today.Month;
                _monthTransCount.TransCount = 0;
            }

            _monthTransCount.TransCount += _totalTransactions;

            var monthStart = new DateTime(2021, today.Month, 1, 0, 0, 1);
            var todayTrans = db.CloudTransactions.Where(x => x.Date == monthStart && x.TransType == _serviceType).FirstOrDefault();

            if (todayTrans == null)
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
}
