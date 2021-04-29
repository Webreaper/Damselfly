using System;
using System.Text;

namespace Damselfly.Core.Utils
{
    public static class StringExtensions
    {
        /// <summary>
        /// Case insensitive contains. Still not really sure why this
        /// isn't something that's part of .Net....
        /// </summary>
        /// <param name="source"></param>
        /// <param name="toCheck"></param>
        /// <returns></returns>
        public static bool ContainsNoCase(this string source, string toCheck)
        {
            bool passes = source?.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase) >= 0;
            return passes;
        }

        /// <summary>
        /// Display a date/time in a format which is locale-agnostic
        /// and looks tidy in the UI. Wrapped in a helper so I can
        /// change this format in a single place if need be.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string Display(this DateTime date)
        {
            return $"{date:dd-MMM-yyy HH:mm:ss}";
        }
        /// <summary>
        /// Strip smart-quotes and replace with single or double-quotes
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string RemoveSmartQuotes(this string s)
        {
            const char singleQuote = '\'';
            const char doubleQuote = '\"';

            var result = s.Replace('\u0091', singleQuote)
                          .Replace('\u0092', singleQuote)
                          .Replace('\u2018', singleQuote)
                          .Replace('\u2019', singleQuote)
                          .Replace('\u201d', doubleQuote)
                          .Replace('\u201c', doubleQuote);

            return result;
        }

        /// <summary>
        /// Creates a human-readable string for a timespan, rounding to the
        /// nearest significant order of magnitude. So something like
        /// "5 seconds", or "2 hours".
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
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

        public static string ToHumanReadableString(this long timeMilliSecs)
        {
            int x = (int)timeMilliSecs;
            return x.ToHumanReadableString();
        }

        public static string ToHumanReadableString(this DateTime start)
        {
            return start.ToHumanReadableString(DateTime.UtcNow);

        }
        public static string ToHumanReadableString(this DateTime start, DateTime end)
        {
            int milliSeconds = (int)((end - start).TotalMilliseconds);
            return milliSeconds.ToHumanReadableString();
        }

        public static string ToHumanReadableString(this int timeMilliSecs)
        {
            var t = new TimeSpan(0, 0, 0, 0, timeMilliSecs);
            return t.ToHumanReadableString();
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
