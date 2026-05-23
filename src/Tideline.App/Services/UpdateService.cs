using System;

namespace Tideline.App.Services;

/// <summary>
/// Placeholder for the launch-time update check described in SPEC section 19.
/// The Velopack package version is intentionally left unpinned until the
/// release pipeline first runs against a real tag; see OPEN_QUESTIONS.md.
/// The shape of this service is what App.OnLaunched calls into so swapping
/// in Velopack later is a one-file change.
/// </summary>
public sealed class UpdateService
{
    public bool IsEnabled { get; set; } = false;
    public string? LastCheckError { get; private set; }

    public void CheckOnceFireAndForget()
    {
        if (!IsEnabled) return;
        // TODO OWNER DECISION: pick Velopack version, then implement
        // UpdateManager check and stage. Must never block app launch.
        // The check should be debounced so tray clicks within a session
        // do not re-check (SPEC section 19.2).
    }
}
