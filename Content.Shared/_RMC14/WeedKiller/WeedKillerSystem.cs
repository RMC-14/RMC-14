using System.Linq;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.CameraShake;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared.Coordinates;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.WeedKiller;

public sealed class WeedKillerSystem : EntitySystem
{
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly RMCCameraShakeSystem _rmcCameraShake = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<DeletedByWeedKillerComponent> _deletedByWeedKillerQuery;

    private static readonly EntProtoId WeedKiller = "RMCGasWeedKiller";
    private TimeSpan _dropshipDelay;
    private TimeSpan _disableDuration;

    public override void Initialize()
    {
        _deletedByWeedKillerQuery = GetEntityQuery<DeletedByWeedKillerComponent>();

        SubscribeLocalEvent<DropshipLaunchedFromWarshipEvent>(OnDropshipLaunchedFromWarship);

        Subs.CVar(_config, RMCCVars.RMCWeedKillerDropshipDelaySeconds, v => _dropshipDelay = TimeSpan.FromSeconds(v), true);
        Subs.CVar(_config, RMCCVars.RMCWeedKillerDisableDurationMinutes, v => _disableDuration = TimeSpan.FromMinutes(v), true);
    }

    private void OnDropshipLaunchedFromWarship(ref DropshipLaunchedFromWarshipEvent ev)
    {
        if (_net.IsClient)
            return;

        if (Count<WeedKillerComponent>() > 0)
            return;

        if (ev.Dropship.Comp.Destination is not { } destination)
            return;

        var coordinates = destination.ToCoordinates();
        if (!_area.TryGetArea(coordinates, out var lzArea, out _) ||
            string.IsNullOrWhiteSpace(lzArea.Value.Comp.LinkedLz))
        {
            return;
        }

        CreateWeedKiller(ev.Dropship, coordinates);
    }

    public void CreateWeedKiller(EntityUid dropship, EntityCoordinates coordinates)
    {
        var id = Spawn();
        var comp = EnsureComp<WeedKillerComponent>(id);
        comp.DeployAt = _timing.CurTime + _dropshipDelay;
        comp.DisableAt = _timing.CurTime + _dropshipDelay + _disableDuration;
        comp.Dropship = dropship;
        Dirty(id, comp);

        if (!_area.TryGetArea(coordinates, out var lzArea, out _))
            return;

        var areas = EntityQueryEnumerator<AreaComponent>();
        while (areas.MoveNext(out var areaId, out var areaComp))
        {
            if (areaComp.LinkedLz?.Contains(',') ?? false)
            {
                if (!areaComp.LinkedLz.Split(',').Select(x => x.Trim()).Contains(lzArea.Value.Comp.LinkedLz))
                    continue;
            }
            else if (areaComp.LinkedLz != lzArea.Value.Comp.LinkedLz)
            {
                continue;
            }

            if (Prototype(areaId)?.ID is { } proto)
                comp.AreaPrototypes.Add(proto);

            comp.LinkedAreas.Add(areaId);
        }

        var gridId = _transform.GetGrid(coordinates);
        if (TryComp(gridId, out MapGridComponent? grid) &&
            TryComp(gridId, out AreaGridComponent? areaGrid))
        {
            foreach (var (position, areaId) in areaGrid.Areas)
            {
                if (comp.AreaPrototypes.Contains(areaId))
                    comp.Positions.Add(((gridId.Value, grid), position));
            }
        }
    }

    private void KillWeeds(Entity<WeedKillerComponent> killer)
    {
        foreach (var areaId in killer.Comp.LinkedAreas)
        {
            if (!TryComp(areaId, out AreaComponent? area))
                continue;

            area.WeedKilling = true;
            Dirty(areaId, area);
        }

        var dropship = killer.Comp.Dropship;

        if (dropship != null)
        {
            var map = _transform.GetMapId(dropship.Value);
            var mapFilter = Filter.BroadcastMap(map);

            _audio.PlayGlobal(killer.Comp.Sound, mapFilter, false);
            _rmcCameraShake.ShakeCamera(mapFilter, 3, 1);
            _marineAnnounce.AnnounceARESStaging(null, Loc.GetString("rmc-weed-killer-deploying", ("dropship", Name(dropship.Value))));
        }

        foreach (var position in killer.Comp.Positions)
        {
            Spawn(WeedKiller, _map.ToCoordinates(position.Grid, position.Indices, position.Grid));
            var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(position.Grid, position.Indices);
            while (anchored.MoveNext(out var anchoredId))
            {
                if (!_deletedByWeedKillerQuery.HasComp(anchoredId))
                    continue;

                QueueDel(anchoredId);
            }
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var killer = EntityQueryEnumerator<WeedKillerComponent>();
        while (killer.MoveNext(out var uid, out var comp))
        {
            if (!comp.Deployed)
            {
                if (time < comp.DeployAt)
                    continue;

                comp.Deployed = true;
                Dirty(uid, comp);
                KillWeeds((uid, comp));
            }

            if (!comp.Disabled)
            {
                if (time < comp.DisableAt)
                    continue;

                comp.Disabled = true;
                Dirty(uid, comp);

                foreach (var areaId in comp.LinkedAreas)
                {
                    if (!TryComp(areaId, out AreaComponent? area))
                        continue;

                    area.WeedKilling = false;
                    Dirty(areaId, area);
                }
            }
        }
    }
}
