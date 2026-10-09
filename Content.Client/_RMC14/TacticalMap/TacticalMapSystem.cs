using Content.Client._RMC14.Dropship.Weapon;
using Content.Client._RMC14.UserInterface;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared._RMC14.Xenonids.Watch;
using Robust.Client.Player;
using Robust.Shared.Map.Components;

namespace Content.Client._RMC14.TacticalMap;

public sealed class TacticalMapSystem : SharedTacticalMapSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<TransformComponent> _transformQuery;
    private EntityQuery<TacticalMapComponent> _tacticalMapQuery;
    private EntityQuery<MapGridComponent> _mapGridQuery;

    public override void Initialize()
    {
        base.Initialize();

        _transformQuery = GetEntityQuery<TransformComponent>();
        _tacticalMapQuery = GetEntityQuery<TacticalMapComponent>();
        _mapGridQuery = GetEntityQuery<MapGridComponent>();

        SubscribeLocalEvent<TacticalMapUserComponent, AfterAutoHandleStateEvent>(OnUserState);
        SubscribeLocalEvent<TacticalMapComputerComponent, AfterAutoHandleStateEvent>(OnComputerState);
        SubscribeLocalEvent<TacticalMapLinesComponent, AfterAutoHandleStateEvent>(OnLinesState);
        SubscribeLocalEvent<TacticalMapLabelsComponent, AfterAutoHandleStateEvent>(OnLabelsState);

        SubscribeLocalEvent<XenoWatchingComponent, XenoWatchEvent>(OnXenoWatch);
        SubscribeLocalEvent<XenoWatchingComponent, XenoUnwatchEvent>(OnXenoUnwatch);
    }

    public override bool HasValidPosition(EntityUid ent,
        ref Vector2i indices)
    {
        return _transformQuery.TryComp(ent, out var xform) &&
               xform.GridUid is { } gridId &&
               _mapGridQuery.TryComp(gridId, out var gridComp) &&
               _tacticalMapQuery.TryComp(gridId, out var tacticalMap) &&
               _transform.TryGetGridTilePosition((ent, xform), out indices, gridComp);
    }

    // watchTargetUid exists to be passed to TacticalMapControl.SetWatchingEntityId() as the EyeComponent.Target
    //     value had race conditions preventing it from always being set in time
    private void RefreshUser(EntityUid ent, EntityUid? watchTargetUid = null, bool? liveUpdate = null)
    {
        try
        {
            if (!TryComp(ent, out UserInterfaceComponent? ui))
                return;

            foreach (var bui in ui.ClientOpenInterfaces.Values)
            {
                if (bui is TacticalMapUserBui userBui)
                    userBui.Refresh(watchTargetUid, liveUpdate);
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {nameof(TacticalMapUserBui)}\n{e}");
        }
    }

    private void RefreshComputer(EntityUid ent, EntityUid? watchTargetUid = null, bool? liveUpdate = null)
    {
        try
        {
            if (!TryComp(ent, out UserInterfaceComponent? ui))
                return;

            foreach (var bui in ui.ClientOpenInterfaces.Values)
            {
                if (bui is TacticalMapComputerBui computerBui)
                    computerBui.Refresh(watchTargetUid, liveUpdate);
                else if (bui is DropshipWeaponsBui weaponsBui)
                    weaponsBui.Refresh();
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

    private void OnXenoWatch(Entity<XenoWatchingComponent> ent, ref XenoWatchEvent args)
    {
        TryComp(ent, out TacticalMapUserComponent? userComponent);

        bool? liveUpdate = userComponent?.LiveUpdate ?? null;

        RefreshUser(ent, args.Watching, liveUpdate);

        if (HasComp<TacticalMapComputerComponent>(ent))
            RefreshComputer(ent, args.Watching, liveUpdate);
    }

    private void OnXenoUnwatch(Entity<XenoWatchingComponent> ent, ref XenoUnwatchEvent args)
    {
        RefreshUser(ent, EntityUid.Invalid);

        if (HasComp<TacticalMapComputerComponent>(ent))
            RefreshComputer(ent, EntityUid.Invalid);
    }
}
