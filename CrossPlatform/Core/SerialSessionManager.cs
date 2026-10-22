using System;
using System.Collections.Generic;
using System.Linq;

namespace BCom.CrossPlatform;

/// <summary>
/// Owns all live serial connections. Port identity is case-insensitive and a
/// disconnected session cannot occupy a slot permanently after a hot unplug.
/// </summary>
public sealed class SerialSessionManager : IDisposable
{
    private readonly List<SerialSession> _sessions = new();

    public IReadOnlyList<SerialSession> Sessions => _sessions;
    public SerialSession? Active { get; private set; }

    public event Action? SessionsChanged;
    public event Action<SerialSession?>? ActiveChanged;

    public SerialSession? Find(string port) =>
        _sessions.FirstOrDefault(s =>
            string.Equals(s.Config.PortName, port, StringComparison.OrdinalIgnoreCase));

    public bool TryOpen(SerialConfig config, out SerialSession? session, out string? error)
    {
        session = null;
        error = null;
        if (string.IsNullOrWhiteSpace(config.PortName))
        {
            error = I18n.T("ErrorNoPort");
            return false;
        }

        var previous = Find(config.PortName);
        if (previous?.IsOpen == true)
        {
            Select(previous);
            session = previous;
            return true;
        }

        if (previous != null) Remove(previous);

        var candidate = new SerialSession(config);
        if (!candidate.Open(out error))
        {
            candidate.Dispose();
            return false;
        }

        _sessions.Add(candidate);
        session = candidate;
        Select(candidate);
        SessionsChanged?.Invoke();
        return true;
    }

    public bool Select(SerialSession? session)
    {
        if (session != null && !_sessions.Contains(session)) return false;
        if (ReferenceEquals(session, Active)) return true;
        Active = session;
        ActiveChanged?.Invoke(Active);
        return true;
    }

    public bool Remove(SerialSession session)
    {
        if (!_sessions.Remove(session)) return false;
        bool wasActive = ReferenceEquals(Active, session);
        session.Dispose();
        if (wasActive)
        {
            Active = null;
            Select(_sessions.LastOrDefault());
        }
        SessionsChanged?.Invoke();
        return true;
    }

    public void Dispose()
    {
        foreach (var session in _sessions.ToArray()) session.Dispose();
        _sessions.Clear();
        Active = null;
    }
}
