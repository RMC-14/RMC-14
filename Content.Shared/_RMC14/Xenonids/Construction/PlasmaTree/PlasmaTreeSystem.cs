using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Construction.PlasmaTree;

public sealed class PlasmaTreeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly XenoPlasmaSystem _plasma = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doafter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlasmaTreeComponent, PlasmaTreeRecoverDoAfterEvent>(OnRecoveryDoAfter);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var curTime = _time.CurTime;
        var plasmaTrees = EntityQueryEnumerator<PlasmaTreeComponent>();

        while (plasmaTrees.MoveNext(out var ent, out var comp))
        {
            if (comp.NextPlasmaAt < curTime && comp.PlasmaDoAfter == null)
            {
                TryGivePlasmaRandomXeno((ent, comp));
            }
        }
    }

    private void TryGivePlasmaRandomXeno(Entity<PlasmaTreeComponent> plasmaTree)
    {
        var (ent, comp) = plasmaTree;
        var nearbyEntities = _lookup.GetEntitiesInRange(ent, comp.PlasmaRange);
        var possibleTargets = new List<EntityUid>();
        foreach (var nearbyEntity in nearbyEntities)
        {
            if (!_hive.FromSameHive(ent, nearbyEntity) ||
                !HasComp<XenoComponent>(nearbyEntity) ||
                !HasComp<XenoRestingComponent>(nearbyEntity) ||
                !TryComp<XenoPlasmaComponent>(nearbyEntity, out var plasmaComp) ||
                plasmaComp.Plasma == plasmaComp.MaxPlasma ||
                !HasComp<MobStateComponent>(nearbyEntity) ||
                _mob.IsDead(nearbyEntity))
            {
                continue;
            }

            possibleTargets.Add(nearbyEntity);
        }

        plasmaTree.Comp.NextPlasmaAt = _time.CurTime + plasmaTree.Comp.PlasmaCooldown;

        if (possibleTargets.Count == 0)
            return;

        var selectedTarget = _random.Pick(possibleTargets);
        var recover = new DoAfterArgs(EntityManager,
            plasmaTree,
            plasmaTree.Comp.PlasmaCooldown,
            new PlasmaTreeRecoverDoAfterEvent(),
            plasmaTree,
            selectedTarget)
        {
            BreakOnMove = true,
            MovementThreshold = 0.5f,
            DuplicateCondition = DuplicateConditions.SameEvent,
            TargetEffect = "RMCEffectHealBusy",
        };

        if (_doafter.TryStartDoAfter(recover, out var id))
        {
            plasmaTree.Comp.PlasmaDoAfter = id;
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-recovery-node-heal-target"),
                selectedTarget,
                selectedTarget);
            _popup.PopupEntity(
                Loc.GetString("rmc-xeno-construction-recovery-node-heal-other", ("target", selectedTarget)),
                selectedTarget,
                Filter.PvsExcept(selectedTarget),
                true);
        }
    }

    private void OnRecoveryDoAfter(Entity<PlasmaTreeComponent> plasmaTree, ref PlasmaTreeRecoverDoAfterEvent args)
    {
        plasmaTree.Comp.PlasmaDoAfter = null;

        if (args.Handled || args.Cancelled || args.Target == null)
            return;

        _plasma.RegenPlasma(args.Target.Value, plasmaTree.Comp.PlasmaAmount);
    }
}
