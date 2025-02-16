using Content.Shared._RMC14.Overwatch;

namespace Content.Client._RMC14.Overwatch;

public sealed class OverwatchConsoleSystem : SharedOverwatchConsoleSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OverwatchConsoleComponent, AfterAutoHandleStateEvent>(OnOverwatchAfterState);
    }

    private void OnOverwatchAfterState(Entity<OverwatchConsoleComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (!TryComp(ent, out UserInterfaceComponent? ui))
                return;

            foreach (var bui in ui.ClientOpenInterfaces.Values)
            {
                if (bui is OverwatchConsoleBui overwatchUi)
                    overwatchUi.Refresh();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {nameof(OverwatchConsoleBui)}\n{e}");
        }
    }
}
