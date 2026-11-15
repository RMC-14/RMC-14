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

    private EntityQuery<XenoWeedsComponent> _weedsQuery;

    private readonly HashSet<Entity<WeedboundWallComponent>> _toDelete = new();

    public override void Initialize()
    {
        _weedsQuery = GetEntityQuery<XenoWeedsComponent>();

        SubscribeLocalEvent<WeedboundWallComponent, DestructionEventArgs>(OnWeedboundDestruction);
        SubscribeLocalEvent<WeedboundWallComponent, ComponentStartup>(OnWeedboundStartup);
        SubscribeLocalEvent<WeedboundWallComponent, MapInitEvent>(OnWeedboundMapInit);
        SubscribeLocalEvent<WeedboundWallComponent, EntityTerminatingEvent>(OnWeedboundTerminating);
        SubscribeLocalEvent<WeedboundWallComponent, ComponentRemove>(OnWeedboundRemove);
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

        // Clean-slate: don't trust any serialized association.
        ent.Comp.BoundWeedUid = null;

        var coords = Transform(ent.Owner).Coordinates;
        using var weeds = _rmcMap.GetAnchoredEntitiesEnumerator<XenoWeedsComponent>(coords);
        if (weeds.MoveNext(out var boundWeedUid) && Exists(boundWeedUid))
            RegisterWeedboundStructure(ent.Owner, boundWeedUid);
    }

    public void RebuildWeedboundForWeeds(EntityUid weedsUid)
    {
        if (_net.IsClient)
            return;

        if (!_weedsQuery.TryComp(weedsUid, out var weedsComp))
            return;

        // Clean-slate: maps may serialize runtime associations; clear and rebuild.
        weedsComp.WeedboundStructures.Clear();

        var coords = Transform(weedsUid).Coordinates;
        using var structures = _rmcMap.GetAnchoredEntitiesEnumerator<WeedboundWallComponent>(coords);
        while (structures.MoveNext(out var structureUid) && Exists(structureUid))
        {
            RegisterWeedboundStructure(structureUid, weedsUid);
        }
    }

    public void RegisterWeedboundStructure(EntityUid structure, EntityUid weed)
    {
        if (_net.IsClient)
            return;

        if (!Exists(structure) || !Exists(weed))
            return;

        if (!TryComp(structure, out WeedboundWallComponent? weedboundWall))
            return;

        if (!_weedsQuery.TryComp(weed, out var weedsComp))
            return;

        // If this structure was previously registered to a different weeds entity, clean that up first.
        if (weedboundWall.BoundWeedUid is { } oldWeed && oldWeed != weed)
        {
            UnregisterWeedboundStructure(structure, weedboundWall);
        }

        if (!weedsComp.WeedboundStructures.Contains(structure))
            weedsComp.WeedboundStructures.Add(structure);

        weedboundWall.BoundWeedUid = weed;

        if (!TryComp(weed, out HiveMemberComponent? weedHiveMember))
            return;

        _hive.SetHive(structure, weedHiveMember.Hive);
    }

    private void SpawnResinResidue(EntityUid structure, EntityCoordinates coords)
    {
        if (!TryComp(structure, out WeedboundWallComponent? weedbound))
            return;

        var residueProto = weedbound.IsThickVariant
            ? weedbound.ThickResiduePrototype
            : weedbound.ResiduePrototype;

        var residue = Spawn(residueProto, coords);

        // Some weedbound structures may not have their hive set (e.g. created by older paths),
        // but their supporting weeds will. Prefer the structure, otherwise fall back to the weeds.
        if (TryComp(structure, out HiveMemberComponent? structureHive) && structureHive.Hive != null)
        {
            _hive.SetSameHive(structure, residue);
            return;
        }

        if (TryComp(structure, out WeedboundWallComponent? wall) &&
            wall.BoundWeedUid is { } boundWeed && Exists(boundWeed) &&
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

        UnregisterWeedboundStructure(ent.Owner, ent.Comp);
    }

    private void OnWeedboundRemove(Entity<WeedboundWallComponent> ent, ref ComponentRemove args)
    {
        if (_net.IsClient)
            return;

        UnregisterWeedboundStructure(ent.Owner, ent.Comp);
    }

    private void UnregisterWeedboundStructure(EntityUid structure, WeedboundWallComponent? weedbound = null)
    {
        if (!Resolve(structure, ref weedbound, false))
            return;

        var weed = weedbound.BoundWeedUid;
        weedbound.BoundWeedUid = null;

        if (weed is not { } weedUid || !Exists(weedUid))
            return;

        if (!_weedsQuery.TryComp(weedUid, out var weeds))
            return;

        weeds.WeedboundStructures.Remove(structure);
    }

    public void HandleWeedsTerminating(EntityUid weedsUid, XenoWeedsComponent weedsComp)
    {
        if (_net.IsClient)
            return;

        if (weedsComp.WeedboundStructures.Count == 0)
            return;

        foreach (var structure in weedsComp.WeedboundStructures.ToArray())
        {
            if (!Exists(structure))
                continue;

            // Defensive: WeedboundStructures is runtime bookkeeping and can become stale due to pooling/serialization.
            // Never delete arbitrary entities unless they are actually weedbound to these weeds.
            if (!TryComp(structure, out WeedboundWallComponent? weedbound) || weedbound.BoundWeedUid != weedsUid)
            {
                weedsComp.WeedboundStructures.Remove(structure);
                continue;
            }

            _toDelete.Add((structure, weedbound));
        }

        weedsComp.WeedboundStructures.Clear();
    }

    public void HandleWeedsShutdown(EntityUid weedsUid, XenoWeedsComponent weedsComp)
    {
        if (_net.IsClient)
            return;

        // Ensure structures don't keep a stale weed reference if weeds are removed first.
        foreach (var structure in weedsComp.WeedboundStructures.ToArray())
        {
            if (!TryComp(structure, out WeedboundWallComponent? weedbound))
                continue;

            if (weedbound.BoundWeedUid == weedsUid)
                weedbound.BoundWeedUid = null;
        }

        weedsComp.WeedboundStructures.Clear();
    }

    public override void Update(float frameTime)
    {
        try
        {
            if (_net.IsClient)
                return;

            foreach (var toDelete in _toDelete)
            {
                if (TerminatingOrDeleted(toDelete))
                    continue;

                // Try to rebind to new weeds
                BindAndRegister(toDelete);

                // New weeds replaced the old ones, don't delete
                if (!TerminatingOrDeleted(toDelete.Comp.BoundWeedUid))
                    continue;

                // If supporting weeds are destroyed, the weedbound structure collapses and leaves sticky resin.
                if (!TryComp(toDelete, out TransformComponent? xform))
                    continue;

                var coords = xform.Coordinates;
                SpawnResinResidue(toDelete, coords);
                QueueDel(toDelete);
            }
        }
        finally
        {
            _toDelete.Clear();
        }
    }
}
