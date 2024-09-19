using Content.Shared._RMC14.TacticalMap;

namespace Content.Client._RMC14.TacticalMap;

public sealed class TacticalMapSystem : SharedTacticalMapSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TacticalMapUserComponent, AfterAutoHandleStateEvent>(OnUserState);
        SubscribeLocalEvent<TacticalMapComputerComponent, AfterAutoHandleStateEvent>(OnComputerState);
    }

    private void OnUserState(Entity<TacticalMapUserComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (!TryComp(ent, out UserInterfaceComponent? ui))
                return;

            foreach (var bui in ui.ClientOpenInterfaces.Values)
            {
                if (bui is TacticalMapUserBui userBui)
                    userBui.Refresh();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {nameof(TacticalMapUserBui)}\n{e}");
        }
    }

    private void OnComputerState(Entity<TacticalMapComputerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (!TryComp(ent, out UserInterfaceComponent? ui))
                return;

            foreach (var bui in ui.ClientOpenInterfaces.Values)
            {
                if (bui is TacticalMapComputerBui computerBui)
                    computerBui.Refresh();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {nameof(TacticalMapComputerBui)}\n{e}");
        }
    }
}
