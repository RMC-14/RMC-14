using System.Linq;
using Content.Shared._RMC14.ARES.Logs;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Clock;
using Content.Shared.Coordinates;
using Content.Shared.GameTicking;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.ARES;

public sealed class RMCARESCoreSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedGameTicker _ticker = default!;
    [Dependency] private readonly INetManager _net = default!;

    private List<Entity<RMCARESCoreComponent>> _cores = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCARESCoreComponent, EntityTerminatingEvent>(OnEntityTerminating);
        SubscribeLocalEvent<RMCARESCoreComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnComponentRemove(Entity<RMCARESCoreComponent> ent, ref ComponentRemove args)
    {
        IndexRemove(ent);
    }

    private void OnEntityTerminating(Entity<RMCARESCoreComponent> ent, ref EntityTerminatingEvent args)
    {
        IndexRemove(ent);
    }

    private void IndexRemove(Entity<RMCARESCoreComponent> ent)
    {
        if (_cores.Contains(ent))
        {
            _cores.Remove(ent);
        }
    }

    /// <summary>
    /// Returns a Core for a specific faction.
    /// </summary>
    /// <param name="faction">Faction ID</param>
    /// <param name="ares">The returned entity</param>
    /// <returns></returns>
    public bool TryGetARES(EntProtoId<IFFFactionComponent> faction, out Entity<RMCARESCoreComponent>? ares)
    {
        foreach (var core in _cores)
        {
            if (core.Comp.Faction != faction)
                continue;
            ares = (core.Owner, core.Comp);
            return true;
        }

        var query = EntityQueryEnumerator<RMCARESCoreComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Faction != faction)
                continue;
            _cores.Add((uid, comp));
            ares = (uid, comp);
            return true;
        }
        ares = null;
        return false;
    }

    /// <summary>
    /// Returns a Core on a specific map.
    /// </summary>
    /// <param name="mapId">The map ID the core is on</param>
    /// <param name="ares">The returned entity</param>
    /// <returns></returns>
    public bool TryGetARES(MapId mapId, out Entity<RMCARESCoreComponent>? ares)
    {

        foreach (var core in _cores)
        {
            if (_transform.GetMapId(core.Owner) != mapId)
                continue;
            ares = (core.Owner, core.Comp);
            return true;
        }

        var query = EntityQueryEnumerator<RMCARESCoreComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_transform.GetMapId(uid) != mapId)
                continue;
            ares = (uid, comp);
            return true;
        }
        ares = null;
        return false;
    }

    /// <summary>
    /// Returns a Core on a specific map.
    /// </summary>
    /// <param name="map">The map the core is on</param>
    /// <param name="ares">The returned entity</param>
    /// <returns></returns>
    public bool TryGetARES(Entity<MapComponent> map, out Entity<RMCARESCoreComponent>? ares)
    {

        foreach (var core in _cores)
        {
            if (_transform.GetMap(core.Owner) != map)
                continue;
            ares = (core.Owner, core.Comp);
            return true;
        }

        var query = EntityQueryEnumerator<RMCARESCoreComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_transform.GetMap(uid) != map)
                continue;
            ares = (uid, comp);
            return true;
        }
        ares = null;
        return false;
    }

    /// <summary>
    /// Returns a Core on a specific map.
    /// </summary>
    /// <param name="entity">The entity on the map shared with the ares core.</param>
    /// <param name="ares">The returned entity</param>
    /// <returns></returns>
    public bool TryGetARES(EntityUid entity, out Entity<RMCARESCoreComponent>? ares)
    {

        foreach (var core in _cores)
        {
            if (_transform.GetMap(core.Owner) != _transform.GetMap(entity))
                continue;
            ares = (core.Owner, core.Comp);
            return true;
        }

        var query = EntityQueryEnumerator<RMCARESCoreComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_transform.GetMap(uid) != _transform.GetMap(entity))
                continue;
            ares = (uid, comp);
            return true;
        }
        ares = null;
        return false;
    }

    // Logs

    public void CreateARESLog( EntProtoId<RMCARESLogTypeComponent> logType, string message, EntityUid aiCore)
    {
        if (!TryComp(aiCore, out RMCARESCoreComponent? core) || _net.IsClient)
            return;

        var query = EntityQuery<GlobalTimeManagerComponent>().FirstOrDefault();
        var worldTime = (query?.TimeOffset ?? TimeSpan.Zero) + _ticker.RoundDuration();
        var worldDate = (query?.DateOffset ?? DateTime.Today.AddYears(100))
                        + worldTime;
        var time = worldDate.ToString("dd - HH:mm");

        var ev = new RMCARESLogUpdateEvent((aiCore, core));
        RaiseLocalEvent(ev);

        core.Logs.GetOrNew(logType).Add($"{time} | {message}");
    }

    public void CreateARESLog(EntityUid entity, EntProtoId<RMCARESLogTypeComponent> logType, string message)
    {
        if (!TryGetARES(entity, out var ares) || ares == null)
            return;

        CreateARESLog(logType, message, ares.Value.Owner);
    }

    public void CreateARESLog(EntProtoId<IFFFactionComponent> faction, EntProtoId<RMCARESLogTypeComponent> logType, string message)
    {
        if (!TryGetARES(faction, out var ares) || ares == null)
            return;

        CreateARESLog(logType, message, ares.Value.Owner);
    }

    public bool PullARESLogs(EntityUid aiCore, EntProtoId<RMCARESLogTypeComponent>? logType, out List<string>? logs)
    {
        if (!TryComp(aiCore, out RMCARESCoreComponent? core))
        {
            logs = null;
            return false;
        }

        if (logType != null && core.Logs.TryGetValue(logType.Value, out var entries))
        {
            logs = entries;
            return true;
        }

        logs = null;
        return false;
    }
}
