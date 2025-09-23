using Content.Shared._RMC14.Intel.Tech;

namespace Content.Client._RMC14.Intel;

public sealed class TechControlConsoleSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<TechControlConsoleComponent, AfterAutoHandleStateEvent>(OnState);
    }

    private void OnState(Entity<TechControlConsoleComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (!TryComp(ent, out UserInterfaceComponent? ui))
                return;

            foreach (var bui in ui.ClientOpenInterfaces.Values)
            {
                if (bui is TechControlConsoleBui consoleUi)
                    consoleUi.Refresh();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {nameof(TechControlConsoleBui)}\n{e}");
        }
    }
}
