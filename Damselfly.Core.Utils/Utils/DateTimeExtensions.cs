using System;

namespace Damselfly.Core.Utils
{
    public static class DateTimeExtensions
    {
        public static TimeSpan? LocalTimeSpanToUTC(this TimeSpan? ts)
        {
            if (ts.HasValue)
            {
                DateTime dt = DateTime.Now.Date.Add(ts.Value);
                DateTime dtUtc = dt.ToUniversalTime();
                TimeSpan tsUtc = dtUtc.TimeOfDay;
                return tsUtc;
            }

            return null;
        }

        public static TimeSpan? UTCTimeSpanToLocal(this TimeSpan? tsUtc)
        {
            if (tsUtc.HasValue)
            {
                DateTime dtUtc = DateTime.UtcNow.Date.Add(tsUtc.Value);
                DateTime dt = dtUtc.ToLocalTime();
                TimeSpan ts = dt.TimeOfDay;
                return ts;
            }

            return null;
        }
    }
}
