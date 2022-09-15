using System;

namespace Damselfly.Core.Utils;

public static class DateUtils
{
    /// <summary>
    ///     Display a date/time in a format which is locale-agnostic
    ///     and looks tidy in the UI. Wrapped in a helper so I can
    ///     change this format in a single place if need be. If the
    ///     date is invalid, display unknown.
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static string Display(this DateTime date)
    {
        if ( date == DateTime.MinValue )
            return "Unknown";

        return $"{date:dd-MMM-yyy HH:mm:ss}";
    }
}