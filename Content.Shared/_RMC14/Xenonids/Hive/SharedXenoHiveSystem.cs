using Content.Shared._RMC14.NightVision;
using Content.Shared._RMC14.Xenonids.Announce;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
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
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedXenoAnnounceSystem _xenoAnnounce = default!;

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

    public void CreateHive(string name)
    {
        if (_net.IsClient)
            return;

        var ent = Spawn(null, MapCoordinates.Nullspace);
        EnsureComp<HiveComponent>(ent);
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

    public void AnnounceNeedsOvipositorToSameHive(Entity<XenoComponent?> xeno)
    {
        if (!Resolve(xeno, ref xeno.Comp, false))
            return;

        if (!TryComp(xeno.Comp.Hive, out HiveComponent? hive) ||
            hive.GotOvipositorPopup)
        {
            return;
        }

        hive.GotOvipositorPopup = true;
        Dirty(xeno.Comp.Hive.Value, hive);

        var msg = "Enough time has passed, we require the Queen in oviposition for evolution.";
        var xenos = EntityQueryEnumerator<ActorComponent, XenoComponent>();
        while (xenos.MoveNext(out var uid, out _, out var otherXeno))
        {
            if (uid == xeno.Owner || xeno.Comp.Hive != otherXeno.Hive)
                continue;

            _popup.PopupEntity(msg, uid, uid, PopupType.LargeCaution);
        }

        _xenoAnnounce.AnnounceToHive(default, xeno.Comp.Hive.Value, msg);
    }

    public bool TryGetTierLimit(Entity<HiveComponent?> hive, int tier, out FixedPoint2 value)
    {
        value = default;
        if (!Resolve(hive, ref hive.Comp, false))
            return false;

        return hive.Comp.TierLimits.TryGetValue(tier, out value);
    }

    public bool TryGetFreeSlots(Entity<HiveComponent?> hive, EntProtoId caste, out int value)
    {
        value = default;
        if (!Resolve(hive, ref hive.Comp, false))
            return false;

        return hive.Comp.FreeSlots.TryGetValue(caste, out value);
    }
}
