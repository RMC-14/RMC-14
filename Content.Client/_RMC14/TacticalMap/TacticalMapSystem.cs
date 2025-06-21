using Content.Shared._RMC14.TacticalMap;
using Robust.Client.Player;

namespace Content.Client._RMC14.TacticalMap;

public sealed class TacticalMapSystem : SharedTacticalMapSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TacticalMapUserComponent, AfterAutoHandleStateEvent>(OnUserState);
        SubscribeLocalEvent<TacticalMapComputerComponent, AfterAutoHandleStateEvent>(OnComputerState);
        SubscribeLocalEvent<TacticalMapLinesComponent, AfterAutoHandleStateEvent>(OnLinesState);
        SubscribeLocalEvent<TacticalMapLabelsComponent, AfterAutoHandleStateEvent>(OnLabelsState);
    }

    private void RefreshUser(EntityUid ent)
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

    private void RefreshComputer(EntityUid ent)
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

    private void OnUserState(Entity<TacticalMapUserComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_player.LocalEntity == ent)
            RefreshUser(ent);
    }

    private void OnComputerState(Entity<TacticalMapComputerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshComputer(ent);
    }

    private void OnLinesState(Entity<TacticalMapLinesComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (HasComp<TacticalMapUserComponent>(ent))
            RefreshUser(ent);

        if (HasComp<TacticalMapComputerComponent>(ent))
            RefreshComputer(ent);
    }

    private void OnLabelsState(Entity<TacticalMapLabelsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (HasComp<TacticalMapUserComponent>(ent))
            RefreshUser(ent);

        if (HasComp<TacticalMapComputerComponent>(ent))
            RefreshComputer(ent);
    }
}
