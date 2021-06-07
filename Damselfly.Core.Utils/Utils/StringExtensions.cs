using System;
using System.Text;
using Humanizer;

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
        /// change this format in a single place if need be. If the
        /// date is invalid, display unknown.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string Display(this DateTime date)
        {
            if (date == DateTime.MinValue)
                return "Unknown";

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
            return t.Humanize();
        }

        public static string ToHumanReadableString(this int timeMilliSecs)
        {
            return new TimeSpan(0,0,0,0,timeMilliSecs).Humanize();
        }

        public static string ToHumanReadableString(this DateTime start)
        {
            return start.ToHumanReadableString(DateTime.UtcNow);

        }
        public static string ToHumanReadableString(this DateTime start, DateTime end)
        {
            return (end - start).Humanize();
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
