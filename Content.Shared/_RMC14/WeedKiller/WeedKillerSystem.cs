using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared.Coordinates;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map.Components;
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
    [Dependency] private readonly SharedRMCMapSystem _rmcMap = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

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
        if (Count<WeedKillerComponent>() > 0)
            return;

        var id = Spawn(WeedKiller, ev.Dropship.Owner.ToCoordinates());
        var comp = EnsureComp<WeedKillerComponent>(id);
        comp.DeployAt = _timing.CurTime + _dropshipDelay;
        comp.DeployAt = _timing.CurTime + _dropshipDelay + _disableDuration;
        comp.DropshipName = Name(ev.Dropship);
        comp.Destination = ev.Dropship.Comp.Destination?.ToCoordinates() ?? default;

        if (_area.TryGetArea(comp.Destination, out var lzArea, out _, out _))
        {
            var areas = EntityQueryEnumerator<AreaComponent>();
            while (areas.MoveNext(out var areaId, out var areaComp))
            {
                if (areaComp.LinkedLz != lzArea.LinkedLz)
                    continue;

                if (Prototype(areaId)?.ID is { } proto)
                    comp.AreaPrototypes.Add(proto);

                comp.LinkedAreas.Add(areaId);
            }

            var gridId = comp.Destination.EntityId;
            if (TryComp(gridId, out MapGridComponent? grid) &&
                TryComp(gridId, out AreaGridComponent? areaGrid))
            {
                foreach (var (position, areaId) in areaGrid.Areas)
                {
                    if (comp.AreaPrototypes.Contains(areaId))
                        comp.Positions.Add(_map.GridTileToLocal(gridId, grid, position));
                }
            }
        }

        Dirty(id, comp);
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var killer = EntityQueryEnumerator<WeedKillerComponent>();
        while (killer.MoveNext(out var uid, out var comp))
        {
            if (!comp.Deployed)
            {
                if (comp.DeployAt <= time)
                    continue;

                comp.Deployed = true;
                Dirty(uid, comp);

                foreach (var areaId in comp.LinkedAreas)
                {
                    if (!TryComp(areaId, out AreaComponent? area))
                        continue;

                    area.ResinAllowed = false;
                    Dirty(areaId, area);
                }

                _audio.PlayPvs(comp.Sound, uid);
                _marineAnnounce.AnnounceARES(null, Loc.GetString("rmc-weed-killer-deploying", ("dropship", comp.DropshipName)));

                foreach (var position in comp.Positions)
                {
                    var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(position);
                    while (anchored.MoveNext(out var anchoredId))
                    {
                        if (!_deletedByWeedKillerQuery.HasComp(anchoredId))
                            continue;

                        QueueDel(anchoredId);
                        Spawn(WeedKiller, position);
                    }
                }
            }

            if (!comp.Disabled)
            {
                if (comp.DisableAt <= time)
                    continue;

                comp.Disabled = true;
                Dirty(uid, comp);

                foreach (var areaId in comp.LinkedAreas)
                {
                    if (!TryComp(areaId, out AreaComponent? area))
                        continue;

                    area.ResinAllowed = false;
                    Dirty(areaId, area);
                }
            }
        }
    }
}
