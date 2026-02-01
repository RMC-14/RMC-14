using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Destructible;
using Robust.Shared.Map;
using Robust.Shared.Network;
using System.Linq;

namespace Content.Shared._RMC14.Xenonids.Designer;

public sealed class WeedboundWallSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;

    private readonly Dictionary<EntityUid, HashSet<EntityUid>> _structuresByWeed = new();
    private readonly Dictionary<EntityUid, EntityUid> _weedByStructure = new();
    private EntityQuery<XenoWeedsComponent> _weedsQuery;

    public override void Initialize()
    {
        _weedsQuery = GetEntityQuery<XenoWeedsComponent>();

        SubscribeLocalEvent<WeedboundWallComponent, DestructionEventArgs>(OnWeedboundDestruction);
        SubscribeLocalEvent<WeedboundWallComponent, ComponentStartup>(OnWeedboundStartup);
        SubscribeLocalEvent<WeedboundWallComponent, MapInitEvent>(OnWeedboundMapInit);
        SubscribeLocalEvent<WeedboundWallComponent, EntityTerminatingEvent>(OnWeedboundTerminating);
        SubscribeLocalEvent<EntityTerminatingEvent>(OnAnyEntityTerminating);
    }

    private void OnWeedboundStartup(Entity<WeedboundWallComponent> ent, ref ComponentStartup args)
    {
        BindAndRegister(ent);
    }

    private void OnWeedboundMapInit(Entity<WeedboundWallComponent> ent, ref MapInitEvent args)
    {
        BindAndRegister(ent);
    }

    private void BindAndRegister(Entity<WeedboundWallComponent> ent)
    {
        if (_net.IsClient)
            return;

        var coords = Transform(ent.Owner).Coordinates;
        using var weeds = _rmcMap.GetAnchoredEntitiesEnumerator<XenoWeedsComponent>(coords);
        if (weeds.MoveNext(out var boundWeedUid) && Exists(boundWeedUid))
            RegisterWeedboundStructure(ent.Owner, boundWeedUid);
    }

    public void RegisterWeedboundStructure(EntityUid structure, EntityUid weed)
    {
        if (_net.IsClient)
            return;

        if (!Exists(structure) || !Exists(weed))
            return;

        if (!_structuresByWeed.TryGetValue(weed, out var set))
        {
            set = new HashSet<EntityUid>();
            _structuresByWeed[weed] = set;
        }

        set.Add(structure);
        _weedByStructure[structure] = weed;
    }

    private void SpawnResinResidue(EntityUid structure, EntityCoordinates coords)
    {
        var residueProto = TryComp(structure, out WeedboundWallComponent? weedbound) && weedbound.IsThickVariant
            ? "XenoStickyResin"
            : "XenoStickyResinWeak";

        var residue = Spawn(residueProto, coords);

        // Some weedbound structures may not have their hive set (e.g. created by older paths),
        // but their supporting weeds will. Prefer the structure, otherwise fall back to the weeds.
        if (TryComp(structure, out HiveMemberComponent? structureHive) && structureHive.Hive != null)
        {
            _hive.SetSameHive(structure, residue);
            return;
        }

        if (_weedByStructure.TryGetValue(structure, out var boundWeed) &&
            TryComp(boundWeed, out HiveMemberComponent? weedHive) && weedHive.Hive != null)
        {
            _hive.SetSameHive(boundWeed, residue);
            return;
        }

        using var weeds = _rmcMap.GetAnchoredEntitiesEnumerator<XenoWeedsComponent>(coords);
        while (weeds.MoveNext(out var weedUid) && Exists(weedUid))
        {
            if (TryComp(weedUid, out HiveMemberComponent? tileWeedHive) && tileWeedHive.Hive != null)
            {
                _hive.SetSameHive(weedUid, residue);
                return;
            }
        }
    }

    private void OnWeedboundDestruction(Entity<WeedboundWallComponent> ent, ref DestructionEventArgs args)
    {
        if (_net.IsClient)
            return;

        var coords = Transform(ent.Owner).Coordinates;
        SpawnResinResidue(ent.Owner, coords);
    }

    private void OnWeedboundTerminating(Entity<WeedboundWallComponent> ent, ref EntityTerminatingEvent args)
    {
        if (_net.IsClient)
            return;

        if (!_weedByStructure.TryGetValue(ent.Owner, out var weed))
            return;

        if (!_structuresByWeed.TryGetValue(weed, out var set))
            return;

        set.Remove(ent.Owner);
        if (set.Count == 0)
            _structuresByWeed.Remove(weed);

        _weedByStructure.Remove(ent.Owner);
    }

    private void OnAnyEntityTerminating(ref EntityTerminatingEvent args)
    {
        if (_net.IsClient)
            return;

        var uid = args.Entity.Owner;

        if (!_weedsQuery.HasComp(uid))
            return;

        if (!_structuresByWeed.TryGetValue(uid, out var set))
            return;

        foreach (var structure in set.ToArray())
        {
            if (!Exists(structure))
                continue;

            // If supporting weeds are destroyed, the weedbound structure collapses and leaves sticky resin.
            var coords = Transform(structure).Coordinates;
            SpawnResinResidue(structure, coords);
            QueueDel(structure);
            _weedByStructure.Remove(structure);
        }

        _structuresByWeed.Remove(uid);
    }
}
