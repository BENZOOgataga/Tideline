using System;

namespace Tideline.Core.Time;

/// <summary>
/// Factual, calm framing per SPEC section 23.
/// No speculation, no second-guessing what the user was doing.
/// </summary>
public static class RelativeTime
{
    public static string Written(long createdAtMs, IClock clock)
    {
        DateTimeOffset now = clock.Now();
        DateTimeOffset then = DateTimeOffset.FromUnixTimeMilliseconds(createdAtMs);

        DateTime nowLocalDay = now.LocalDateTime.Date;
        DateTime thenLocalDay = then.LocalDateTime.Date;
        int dayDiff = (nowLocalDay - thenLocalDay).Days;

        if (dayDiff <= 0)
        {
            return "Written today.";
        }
        if (dayDiff == 1)
        {
            return "Written yesterday.";
        }
        if (dayDiff < 14)
        {
            return $"Written {dayDiff} days ago.";
        }
        if (dayDiff < 60)
        {
            int weeks = dayDiff / 7;
            return weeks == 1 ? "Written 1 week ago." : $"Written {weeks} weeks ago.";
        }
        if (dayDiff < 365)
        {
            int months = dayDiff / 30;
            return months == 1 ? "Written 1 month ago." : $"Written {months} months ago.";
        }
        int years = dayDiff / 365;
        return years == 1 ? "Written 1 year ago." : $"Written {years} years ago.";
    }

    public static string ReminderOrDue(long whenMs, string label, IClock clock)
    {
        DateTimeOffset now = clock.Now();
        DateTimeOffset then = DateTimeOffset.FromUnixTimeMilliseconds(whenMs);

        DateTime nowLocalDay = now.LocalDateTime.Date;
        DateTime thenLocalDay = then.LocalDateTime.Date;
        int dayDiff = (thenLocalDay - nowLocalDay).Days;

        if (dayDiff == 0)
        {
            return $"{label} for today.";
        }
        if (dayDiff == 1)
        {
            return $"{label} for tomorrow.";
        }
        if (dayDiff == -1)
        {
            return $"{label} was yesterday.";
        }
        if (dayDiff > 0 && dayDiff < 14)
        {
            return $"{label} in {dayDiff} days.";
        }
        if (dayDiff < 0 && dayDiff > -14)
        {
            return $"{label} {-dayDiff} days ago.";
        }
        return $"{label} on {then.LocalDateTime:MMM d, yyyy}.";
    }
}
