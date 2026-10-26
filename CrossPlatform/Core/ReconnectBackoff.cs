using System;

namespace BCom.CrossPlatform;

/// <summary>Bounded retry policy for reconnecting a physical serial port.</summary>
public sealed class ReconnectBackoff
{
    public int Failures { get; private set; }
    public DateTimeOffset NextAttempt { get; private set; } = DateTimeOffset.MinValue;
    public bool IsDue(DateTimeOffset now) => now >= NextAttempt;
    public void Failed(DateTimeOffset now)
    {
        NextAttempt = now.AddSeconds(Math.Min(30, 2 * (1 << Math.Min(Failures, 4))));
        if (Failures < 100) Failures++;
    }
    public void Reset() { Failures = 0; NextAttempt = DateTimeOffset.MinValue; }
}
