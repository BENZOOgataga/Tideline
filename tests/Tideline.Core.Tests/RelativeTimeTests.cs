using System;
using Tideline.Core.Time;
using Xunit;

namespace Tideline.Core.Tests;

public class RelativeTimeTests
{
    // Build "now" / "then" as local DateTimeOffsets so RelativeTime's
    // local-day comparisons are stable across machine time zones.
    private static FixedClock At(int year, int month, int day, int hour = 12)
        => new(LocalMs(year, month, day, hour));

    private static long LocalMs(int year, int month, int day, int hour = 12)
        => new DateTimeOffset(new DateTime(year, month, day, hour, 0, 0, DateTimeKind.Local)).ToUnixTimeMilliseconds();

    [Fact]
    public void Written_today_when_same_day()
    {
        FixedClock clock = At(2026, 5, 23);
        Assert.Equal("Written today.", RelativeTime.Written(LocalMs(2026, 5, 23, 9), clock));
    }

    [Fact]
    public void Written_yesterday()
    {
        FixedClock clock = At(2026, 5, 23);
        Assert.Equal("Written yesterday.", RelativeTime.Written(LocalMs(2026, 5, 22, 9), clock));
    }

    [Fact]
    public void Written_n_days_ago()
    {
        FixedClock clock = At(2026, 5, 23);
        Assert.Equal("Written 5 days ago.", RelativeTime.Written(LocalMs(2026, 5, 18, 9), clock));
    }

    [Fact]
    public void Reminder_for_today()
    {
        FixedClock clock = At(2026, 5, 23);
        Assert.Equal("Reminder for today.", RelativeTime.ReminderOrDue(LocalMs(2026, 5, 23, 18), "Reminder", clock));
    }

    [Fact]
    public void Reminder_for_tomorrow()
    {
        FixedClock clock = At(2026, 5, 23);
        Assert.Equal("Reminder for tomorrow.", RelativeTime.ReminderOrDue(LocalMs(2026, 5, 24, 9), "Reminder", clock));
    }
}
