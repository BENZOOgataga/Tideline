using System;

namespace Tideline.Core.Time;

/// <summary>
/// Quick snooze targets per SPEC section 11.1: plus one hour, tonight,
/// tomorrow, next week, custom. "Tonight" lands at 20:00 local time;
/// "Tomorrow" and "Next week" land at 09:00 local time.
/// </summary>
public static class SnoozeOptions
{
    public const int TonightHour = 20;
    public const int MorningHour = 9;

    public static long PlusOneHour(IClock clock)
        => clock.Now().AddHours(1).ToUnixTimeMilliseconds();

    public static long Tonight(IClock clock)
    {
        DateTimeOffset now = clock.Now();
        DateTimeOffset localNow = now.ToLocalTime();
        DateTimeOffset target = new(localNow.Year, localNow.Month, localNow.Day, TonightHour, 0, 0, localNow.Offset);
        if (target <= localNow)
        {
            target = target.AddDays(1);
        }
        return target.ToUniversalTime().ToUnixTimeMilliseconds();
    }

    public static long Tomorrow(IClock clock)
    {
        DateTimeOffset localNow = clock.Now().ToLocalTime();
        DateTimeOffset tomorrow = new(localNow.Year, localNow.Month, localNow.Day, MorningHour, 0, 0, localNow.Offset);
        tomorrow = tomorrow.AddDays(1);
        return tomorrow.ToUniversalTime().ToUnixTimeMilliseconds();
    }

    public static long NextWeek(IClock clock)
    {
        DateTimeOffset localNow = clock.Now().ToLocalTime();
        DateTimeOffset nextWeek = new(localNow.Year, localNow.Month, localNow.Day, MorningHour, 0, 0, localNow.Offset);
        nextWeek = nextWeek.AddDays(7);
        return nextWeek.ToUniversalTime().ToUnixTimeMilliseconds();
    }
}
