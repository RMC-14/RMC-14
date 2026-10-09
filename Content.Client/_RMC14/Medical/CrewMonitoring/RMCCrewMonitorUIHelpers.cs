using Content.Shared.Mobs;

namespace Content.Client._RMC14.Medical.CrewMonitoring;

public static class RMCCrewMonitorUIHelpers
{
    public static string GetStatusName(MobState state)
    {
        return state switch
        {
            MobState.Dead => Loc.GetString("rmc-crew-monitor-status-dead"),
            MobState.Critical => Loc.GetString("rmc-crew-monitor-status-critical"),
            _ => Loc.GetString("rmc-crew-monitor-status-alive"),
        };
    }

    public static Color GetStatusColor(MobState state)
    {
        return state switch
        {
            MobState.Dead => Color.FromHex("#FF5C5C"),
            MobState.Critical => Color.FromHex("#FFB347"),
            _ => Color.FromHex("#56D364"),
        };
    }
}
