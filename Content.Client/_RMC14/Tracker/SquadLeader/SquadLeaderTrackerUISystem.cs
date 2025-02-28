using Content.Shared._RMC14.Tracker.SquadLeader;

namespace Content.Client._RMC14.Tracker.SquadLeader;

public sealed class SquadLeaderTrackerUISystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<SquadLeaderTrackerComponent, AfterAutoHandleStateEvent>(OnOverwatchAfterState);
    }

    private void OnOverwatchAfterState(Entity<SquadLeaderTrackerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (!TryComp(ent, out UserInterfaceComponent? ui))
                return;

            foreach (var bui in ui.ClientOpenInterfaces.Values)
            {
                if (bui is SquadInfoBui squadInfoUi)
                    squadInfoUi.Refresh();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {nameof(SquadInfoBui)}\n{e}");
        }
    }
}
