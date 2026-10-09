using Content.Shared._RMC14.Medical.CrewMonitoring;

namespace Content.Client._RMC14.Medical.CrewMonitoring;

public sealed class RMCCrewMonitorSystem : SharedRMCCrewMonitorSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCCrewMonitorComponent, AfterAutoHandleStateEvent>(OnState);
    }

    private void OnState(Entity<RMCCrewMonitorComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (!TryComp(ent, out UserInterfaceComponent? ui))
                return;

            foreach (var bui in ui.ClientOpenInterfaces.Values)
            {
                if (bui is RMCCrewMonitorBui monitor)
                    monitor.Refresh();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {nameof(RMCCrewMonitorBui)}\n{e}");
        }
    }
}
