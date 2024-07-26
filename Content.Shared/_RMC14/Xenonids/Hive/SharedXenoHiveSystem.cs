using Content.Shared._RMC14.NightVision;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared.Mobs;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Xenonids.Hive;

public abstract class SharedXenoHiveSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedNightVisionSystem _nightVision = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<HiveComponent> _query;
    private EntityQuery<HiveMemberComponent> _memberQuery;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<HiveComponent>();
        _memberQuery = GetEntityQuery<HiveMemberComponent>();

        SubscribeLocalEvent<HiveComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<XenoEvolutionGranterComponent, MobStateChangedEvent>(OnGranterMobStateChanged);
    }

    private void OnGranterMobStateChanged(Entity<XenoEvolutionGranterComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (TryComp(ent, out XenoComponent? xeno) &&
            _query.TryComp(xeno.Hive, out var hive))
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

    /// <summary>
    /// Tries to get the hive from a member, returning null if it has no hive or it is invalid.
    /// </summary>
    /// <remarks>
    /// TODO: remove Hive from XenoComponent so this can be used with xenos too.
    /// </remarks>
    public Entity<HiveComponent>? GetHive(Entity<HiveMemberComponent?> member)
    {
        if (!_memberQuery.Resolve(member, ref member.Comp))
            return null;

        if (member.Comp.Hive is not {} uid || TerminatingOrDeleted(uid))
            return null;

        if (!_query.TryComp(uid, out var comp))
            return null;

        return (uid, comp);
    }

    /// <summary>
    /// Sets the hive for a member, if it is different.
    /// If it does not have HiveMemberComponent this method adds it.
    /// </summary>
    public void SetHive(Entity<HiveMemberComponent?> member, EntityUid? hive)
    {
        var comp = member.Comp ?? EnsureComp<HiveMemberComponent>(member);

        if (comp.Hive == hive)
            return;

        comp.Hive = hive;
        Dirty(member);
    }

    public void SetSeeThroughContainers(Entity<HiveComponent?> hive, bool see)
    {
        if (!_query.Resolve(hive, ref hive.Comp, false))
            return;

        hive.Comp.SeeThroughContainers = see;
        var xenos = EntityQueryEnumerator<XenoComponent, NightVisionComponent>();
        while (xenos.MoveNext(out var uid, out var xeno, out var nv))
        {
            if (xeno.Hive != hive)
                continue;

            _nightVision.SetSeeThroughContainers((uid, nv), see);
        }
    }

    /// <summary>
    /// Reserve a construct id, preventing construction of the same type if the limit reaches 0.
    /// </summary>
    public bool ReserveConstruct(Entity<HiveComponent> hive, EntProtoId id)
    {
        var limits = hive.Comp.ConstructionLimits;
        if (!limits.TryGetValue(id, out var limit))
        {
            Log.Error($"Tried to reserve a construct {id} that was not specified in the hive prototype!");
            return false;
        }

        if (limit < 1)
            return false;

        limits[id] = limit - 1;
        Dirty(hive);
        return true;
    }

    /// <summary>
    /// Check if a new construct can be made without reserving one.
    /// </summary>
    public bool CanConstruct(Entity<HiveComponent> hive, EntProtoId id)
    {
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
    public void AdjustConstructLimit(Entity<HiveComponent> hive, EntProtoId id, int add)
    {
        var limits = hive.Comp.ConstructionLimits;
        if (!limits.TryGetValue(id, out var limit))
        {
            Log.Error($"Tried to adjust a construct {id}'s limit that was not specified in the hive prototype!");
            return;
        }

        limits[id] = limit + add;
        Dirty(hive);
    }

    /// <summary>
    /// Checks if constructing is on cooldown from the hive core being destroyed.
    /// </summary>
    public bool IsConstructionOnCooldown(Entity<HiveComponent> hive)
    {
        if (hive.Comp.NextConstructAllowed is not {} cooldown)
            return false;

        return _timing.CurTime < cooldown;
    }

    /// <summary>
    /// Used by hive core system when hive core dies to set construction cooldown.
    /// </summary>
    public void StartCoreDeathCooldown(Entity<HiveComponent> hive, TimeSpan cooldown)
    {
        hive.Comp.NextConstructAllowed = _timing.CurTime + cooldown;
        Dirty(hive);
    }
}
