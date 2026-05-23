using System;

namespace Tideline.Core.Time;

public interface IClock
{
    long NowMs();
    DateTimeOffset Now();
}

public sealed class SystemClock : IClock
{
    public long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public DateTimeOffset Now() => DateTimeOffset.UtcNow;
}

public sealed class FixedClock(long fixedMs) : IClock
{
    private long _ms = fixedMs;
    public void Set(long ms) => _ms = ms;
    public void Advance(TimeSpan ts) => _ms += (long)ts.TotalMilliseconds;
    public long NowMs() => _ms;
    public DateTimeOffset Now() => DateTimeOffset.FromUnixTimeMilliseconds(_ms);
}
