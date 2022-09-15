using System;

namespace Damselfly.Core.Utils;

public static class DateTimeExtensions
{
    public static TimeSpan? LocalTimeSpanToUTC(this TimeSpan? ts)
    {
        if ( ts.HasValue )
        {
            var dt = DateTime.Now.Date.Add(ts.Value);
            var dtUtc = dt.ToUniversalTime();
            var tsUtc = dtUtc.TimeOfDay;
            return tsUtc;
        }

        return null;
    }

    public static TimeSpan? UTCTimeSpanToLocal(this TimeSpan? tsUtc)
    {
        if ( tsUtc.HasValue )
        {
            var dtUtc = DateTime.UtcNow.Date.Add(tsUtc.Value);
            var dt = dtUtc.ToLocalTime();
            var ts = dt.TimeOfDay;
            return ts;
        }

        return null;
    }
}