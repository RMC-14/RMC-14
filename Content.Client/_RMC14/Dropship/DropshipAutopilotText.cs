using Content.Client._RMC14.UserInterface.Crt;
using Content.Shared._RMC14.Dropship;

namespace Content.Client._RMC14.Dropship;

internal static class DropshipAutopilotText
{
    public static string Mode(DropshipAutopilotMode mode)
    {
        return Loc.GetString(mode switch
        {
            DropshipAutopilotMode.Cycle => "rmc-dropship-remote-ui-cycle",
            DropshipAutopilotMode.RecallOnly => "rmc-dropship-remote-ui-recall-only",
            _ => "rmc-dropship-mode-disabled",
        });
    }

    public static string Status(DropshipAutopilotStatus status)
    {
        return Loc.GetString(status switch
        {
            DropshipAutopilotStatus.Ready => "rmc-dropship-autopilot-status-ready",
            DropshipAutopilotStatus.Waiting => "rmc-dropship-autopilot-status-waiting",
            DropshipAutopilotStatus.InFlight => "rmc-dropship-autopilot-status-in-flight",
            DropshipAutopilotStatus.Blocked => "rmc-dropship-autopilot-status-blocked",
            DropshipAutopilotStatus.Error => "rmc-dropship-autopilot-status-error",
            _ => "rmc-dropship-autopilot-status-offline",
        });
    }

    public static RMCCrtTone Tone(DropshipAutopilotStatus status)
    {
        return status switch
        {
            DropshipAutopilotStatus.Ready => RMCCrtTone.Good,
            DropshipAutopilotStatus.Waiting or DropshipAutopilotStatus.Blocked => RMCCrtTone.Warning,
            DropshipAutopilotStatus.Error => RMCCrtTone.Danger,
            DropshipAutopilotStatus.Offline => RMCCrtTone.Muted,
            _ => RMCCrtTone.Default,
        };
    }

    public static string Details(string details, int? departInSeconds)
    {
        return departInSeconds is { } seconds
            ? Loc.GetString("rmc-dropship-departure-countdown", ("details", details), ("seconds", seconds))
            : details;
    }
}
