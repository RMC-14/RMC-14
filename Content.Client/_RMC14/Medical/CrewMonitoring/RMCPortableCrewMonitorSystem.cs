using Content.Shared._RMC14.Medical.CrewMonitoring;

namespace Content.Client._RMC14.Medical.CrewMonitoring;

public sealed class RMCPortableCrewMonitorSystem : SharedRMCPortableCrewMonitorSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCPortableCrewMonitorComponent, AfterAutoHandleStateEvent>(OnMonitorState);
        SubscribeLocalEvent<RMCPortableCrewMonitorTrackingComponent, AfterAutoHandleStateEvent>(OnTrackingState);
    }

    private void OnMonitorState(Entity<RMCPortableCrewMonitorComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            Refresh(ent.Owner, true);
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {nameof(RMCPortableCrewMonitorBui)}\n{e}");
        }
    }

    private void OnTrackingState(Entity<RMCPortableCrewMonitorTrackingComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            Refresh(ent.Owner, false);
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing tracking for {nameof(RMCPortableCrewMonitorBui)}\n{e}");
        }
    }

    private void Refresh(EntityUid uid, bool signals)
    {
        if (!TryComp(uid, out UserInterfaceComponent? ui))
            return;

        foreach (var bui in ui.ClientOpenInterfaces.Values)
        {
            if (bui is not RMCPortableCrewMonitorBui monitor)
                continue;

            if (signals)
                monitor.Refresh();
            else
                monitor.RefreshTracking();
        }
    }
}
