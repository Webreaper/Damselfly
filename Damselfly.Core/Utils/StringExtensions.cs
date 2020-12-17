using System;
using System.Text;

namespace Damselfly.Core.Utils
{
    public static class StringExtensions
    {
        public static bool ContainsNoCase(this string source, string toCheck)
        {
            bool passes = source?.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase) >= 0;
            return passes;
        }

        public static string ToHumanReadableString(this long timeMilliSecs)
        {
            int x = (int)timeMilliSecs;
            return x.ToHumanReadableString();
        }

        public static string Display(this DateTime date)
        {
            return $"{date:dd-MMM-yyy HH:mm:ss}";
        }

        public static string ToHumanReadableString(this DateTime start )
        {
            return start.ToHumanReadableString(DateTime.UtcNow);

        }
        public static string ToHumanReadableString(this DateTime start, DateTime end )
        {
            int milliSeconds = (int)((end - start).TotalMilliseconds);
            return milliSeconds.ToHumanReadableString();
        }

        public static string ToHumanReadableString(this int timeMilliSecs)
        {
            var t = new TimeSpan(0, 0, 0, 0, timeMilliSecs);
            return t.ToHumanReadableString();
        }

        public static string ToHumanReadableString(this TimeSpan t)
        {
            int totalWeeks = (int)(t.TotalDays / 7);
            int totalYears = totalWeeks / 52;

            if (totalYears >= 1)
            {
                return $@"{totalYears} year" + ((int)totalYears > 1 ? "s" : string.Empty);
            }
            if (totalWeeks >= 1)
            {
                return $@"{totalWeeks} week" + ((int)totalWeeks > 1 ? "s" : string.Empty);
            }
            if (t.TotalDays >= 1)
            {
                return $@"{t:%d} day" + ((int)t.TotalDays > 1 ? "s" : string.Empty);
            }
            if (t.TotalHours >= 1)
            {
                return $@"{t:%h} hour" + ((int)t.TotalHours > 1 ? "s" : string.Empty);
            }

            if (t.TotalMinutes >= 2 )
            {
                return $@"{t:%m} minutes";
            }

            if (t.TotalSeconds >= 1)
            {
                int secs = (int)t.TotalSeconds;
                return $@"{secs} second" + (secs > 1 ? "s" : string.Empty);
            }

            return $@"{t:s\.ff}ms";
        }

        /// <summary>
        /// Convert a hash to a string
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="upperCase"></param>
        /// <returns></returns>
        public static string ToHex(this byte[] bytes, bool upperCase)
        {
            StringBuilder result = new StringBuilder(bytes.Length * 2);

            for (int i = 0; i < bytes.Length; i++)
                result.Append(bytes[i].ToString(upperCase ? "X2" : "x2"));

            return result.ToString();
        }
    }
}
