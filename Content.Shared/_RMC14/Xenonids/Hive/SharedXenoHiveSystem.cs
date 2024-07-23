using Content.Shared._RMC14.NightVision;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared.Mobs;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Xenonids.Hive;

public abstract class SharedXenoHiveSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedNightVisionSystem _nightVision = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HiveComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<XenoEvolutionGranterComponent, MobStateChangedEvent>(OnGranterMobStateChanged);
    }

    private void OnGranterMobStateChanged(Entity<XenoEvolutionGranterComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (TryComp(ent, out XenoComponent? xeno) &&
            TryComp(xeno.Hive, out HiveComponent? hive))
        {
            hive.LastQueenDeath = _timing.CurTime;
            Dirty(xeno.Hive.Value, hive);
        }
    }

    private void OnMapInit(Entity<HiveComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.AnnouncedUnlocks.Clear();
        ent.Comp.Unlocks.Clear();
        ent.Comp.AnnouncementsLeft.Clear();

        foreach (var prototype in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (prototype.TryGetComponent(out XenoComponent? xeno, _compFactory))
            {
                if (xeno.UnlockAt == default)
                    continue;

                ent.Comp.Unlocks.GetOrNew(xeno.UnlockAt).Add(prototype.ID);

                if (!ent.Comp.AnnouncementsLeft.Contains(xeno.UnlockAt))
                    ent.Comp.AnnouncementsLeft.Add(xeno.UnlockAt);
            }
        }

        foreach (var unlock in ent.Comp.Unlocks)
        {
            unlock.Value.Sort();
        }

        ent.Comp.AnnouncementsLeft.Sort();
    }

    public void CreateHive(string name, EntProtoId? proto)
    {
        if (_net.IsClient)
            return;

        var ent = Spawn(proto ?? "CMXenoHive", MapCoordinates.Nullspace);
        _metaData.SetEntityName(ent, name);
    }

    public void SetHive(Entity<HiveMemberComponent> member, EntityUid? hive)
    {
        if (member.Comp.Hive == hive)
            return;

        member.Comp.Hive = hive;
        Dirty(member);
    }

    public void SetSeeThroughContainers(Entity<HiveComponent?> hive, bool see)
    {
        if (!Resolve(hive, ref hive.Comp, false))
            return;

        hive.Comp.SeeThroughContainers = see;
        var xenos = EntityQueryEnumerator<XenoComponent>();
        while (xenos.MoveNext(out var uid, out var xeno))
        {
            if (xeno.Hive != hive)
                continue;

            _nightVision.SetSeeThroughContainers(uid, see);
        }
    }

    /// <summary>
    /// Reserve a construct id, preventing construction of the same type if the limit reaches 0.
    /// </summary>
    public bool ReserveConstruct(Entity<HiveComponent?>? ent, EntProtoId id)
    {
        if (ent is not {} hive || !Resolve(hive, ref hive.Comp))
            return false;

        var limits = hive.Comp.ConstructionLimits;
        if (!limits.TryGetValue(id, out var limit))
        {
            Log.Error($"Tried to reserve a construct {id} that was not specified in the hive prototype!");
            return false;
        }

        if (limit < 1)
            return false;

        limits[id] = limit - 1;
        Dirty(hive, hive.Comp);
        return true;
    }

    /// <summary>
    /// Check if a new construct can be made without reserving one.
    /// </summary>
    public bool CanConstruct(Entity<HiveComponent?>? ent, EntProtoId id)
    {
        if (ent is not {} hive || !Resolve(hive, ref hive.Comp))
            return false;

        var limits = hive.Comp.ConstructionLimits;
        if (!limits.TryGetValue(id, out var limit))
        {
            Log.Error($"Tried to check if a construct {id} can be made that was not specified in the hive prototype!");
            return false;
        }

        return limit > 0;
    }

    /// <summary>
    /// Adjust a construct's limit by some value.
    /// </summary>
    /// <remarks>
    /// Intentionally allows going negative to prevent exploiting something and making infinite structures, where going 0 could allow it.
    /// </remarks>
    public void AdjustConstructLimit(Entity<HiveComponent?>? ent, EntProtoId id, int add)
    {
        if (ent is not {} hive || !Resolve(hive, ref hive.Comp))
            return;

        var limits = hive.Comp.ConstructionLimits;
        if (!limits.TryGetValue(id, out var limit))
        {
            Log.Error($"Tried to adjust a construct {id}'s limit that was not specified in the hive prototype!");
            return;
        }

        limits[id] = limit + add;
        Dirty(hive, hive.Comp);
    }
}
