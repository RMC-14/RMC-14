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

public sealed class ARESCoreSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedGameTicker _ticker = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;


    private List<Entity<ARESCoreComponent>> _cores = new();
    private EntityUid? _storedCore;

    private static readonly EntProtoId<IFFFactionComponent> MarineFaction = "FactionMarine";

    public override void Initialize()
    {
        SubscribeLocalEvent<ARESCoreComponent, EntityTerminatingEvent>(OnEntityTerminating);
        SubscribeLocalEvent<ARESCoreComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnComponentRemove(Entity<ARESCoreComponent> ent, ref ComponentRemove args)
    {
        IndexRemove(ent);
    }

    private void OnEntityTerminating(Entity<ARESCoreComponent> ent, ref EntityTerminatingEvent args)
    {
        IndexRemove(ent);
    }

    private void IndexRemove(Entity<ARESCoreComponent> ent)
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
    public bool TryGetARES(EntProtoId<IFFFactionComponent> faction, out Entity<ARESCoreComponent>? ares)
    {
        foreach (var core in _cores)
        {
            if (core.Comp.Faction != faction)
                continue;
            ares = (core.Owner, core.Comp);
            return true;
        }

        var query = EntityQueryEnumerator<ARESCoreComponent>();
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
    public bool TryGetARES(MapId mapId, out Entity<ARESCoreComponent>? ares)
    {
        foreach (var core in _cores)
        {
            if (_transform.GetMapId(core.Owner) != mapId)
                continue;
            ares = (core.Owner, core.Comp);
            return true;
        }

        var query = EntityQueryEnumerator<ARESCoreComponent>();
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
    public bool TryGetARES(Entity<MapComponent> map, out Entity<ARESCoreComponent>? ares)
    {
        foreach (var core in _cores)
        {
            if (_transform.GetMap(core.Owner) != map)
                continue;
            ares = (core.Owner, core.Comp);
            return true;
        }

        var query = EntityQueryEnumerator<ARESCoreComponent>();
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
    public bool TryGetARES(EntityUid entity, out Entity<ARESCoreComponent>? ares)
    {
        foreach (var core in _cores)
        {
            if (_transform.GetMap(core.Owner) != _transform.GetMap(entity))
                continue;
            ares = (core.Owner, core.Comp);
            return true;
        }

        var query = EntityQueryEnumerator<ARESCoreComponent>();
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

    /// <summary>
    /// Will return the marine ARES core if none exists it will spawn an entity with the name ARES v3.2 and use it for announcements.
    /// </summary>
    /// <returns></returns>
    [Obsolete]
    public EntityUid EnsureMarineARES()
    {
        if (TryGetARES(MarineFaction , out var alert) && alert != null)
            return alert.Value;

        if (_storedCore != null)
            return _storedCore.Value;

        var uid = Spawn();
        _metaData.SetEntityName(uid, "ARES v3.2");
        _storedCore = uid;
        return (uid);
    }

    // Logs

    public void CreateARESLog(EntProtoId<ARESLogTypeComponent> logType, string message, EntityUid aiCore)
    {
        if (!TryComp(aiCore, out ARESCoreComponent? core) || _net.IsClient)
            return;

        var query = EntityQuery<GlobalTimeManagerComponent>().FirstOrDefault();
        var worldTime = (query?.TimeOffset ?? TimeSpan.Zero) + _ticker.RoundDuration();
        var worldDate = (query?.DateOffset ?? DateTime.Today.AddYears(100))
                        + worldTime;
        var time = worldDate.ToString("dd - HH:mm");

        var ev = new ARESLogUpdateEvent((aiCore, core));
        RaiseLocalEvent(ev);

        core.Logs.GetOrNew(logType).Add($"{time} | {message}");
    }

    public void CreateARESLog(EntityUid entity, EntProtoId<ARESLogTypeComponent> logType, string message)
    {
        if (!TryGetARES(entity, out var ares) || ares == null)
            return;

        CreateARESLog(logType, message, ares.Value.Owner);
    }

    public void CreateARESLog(EntProtoId<IFFFactionComponent> faction,
        EntProtoId<ARESLogTypeComponent> logType,
        string message)
    {
        if (!TryGetARES(faction, out var ares) || ares == null)
            return;

        CreateARESLog(logType, message, ares.Value.Owner);
    }

    public bool PullARESLogs(EntityUid aiCore, EntProtoId<ARESLogTypeComponent>? logType, out List<string>? logs)
    {
        if (!TryComp(aiCore, out ARESCoreComponent? core))
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
