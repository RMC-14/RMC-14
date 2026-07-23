using Content.Shared._RMC14.Xenonids.Heal;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Construction.RecoveryNode;

public sealed partial class RecoveryNodeSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly SharedDoAfterSystem _doafter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedXenoHealSystem _heal = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly XenoPlasmaSystem _plasma = default!;

    private EntityQuery<XenoComponent> _xenoQuery;
    private EntityQuery<XenoRestingComponent> _restingQuery;
    private EntityQuery<DamageableComponent> _damageableQuery;
    private EntityQuery<XenoPlasmaComponent> _plasmaQuery;

    public override void Initialize()
    {
        base.Initialize();

        _xenoQuery = GetEntityQuery<XenoComponent>();
        _restingQuery = GetEntityQuery<XenoRestingComponent>();
        _damageableQuery = GetEntityQuery<DamageableComponent>();
        _plasmaQuery = GetEntityQuery<XenoPlasmaComponent>();

        SubscribeLocalEvent<RecoveryNodeComponent, RecoveryNodeRecoverDoAfterEvent>(OnRecoveryDoAfter);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var curTime = _time.CurTime;
        var recoverNodes = EntityQueryEnumerator<RecoveryNodeComponent>();

        while (recoverNodes.MoveNext(out var ent, out var comp))
        {
            if (comp.NextRecoveryAt < curTime && comp.DoAfter == null)
            {
                TryRecoverRandomXeno((ent, comp));
            }
        }
    }

    private void TryRecoverRandomXeno(Entity<RecoveryNodeComponent> recoveryNode)
    {
        var nearbyEntities = _lookup.GetEntitiesInRange(recoveryNode, recoveryNode.Comp.Range);
        var possibleTargets = new List<EntityUid>();
        foreach (var nearbyEntity in nearbyEntities)
        {
            if (!_hive.FromSameHive(recoveryNode.Owner, nearbyEntity) ||
                !_xenoQuery.HasComp(nearbyEntity) ||
                !_restingQuery.HasComp(nearbyEntity) ||
                _mob.IsDead(nearbyEntity))
            {
                continue;
            }

            // Extra conditions depending on the recovery type.
            switch (recoveryNode.Comp.RecoveryType)
            {
                case RecoveryType.Health:
                    if (!_damageableQuery.TryComp(nearbyEntity, out var damageComp) || damageComp.TotalDamage <= 0)
                        continue; // continue the foreach loop
                    break; // break the switch statement, not the foreach loop

                case RecoveryType.Plasma:
                    if (!_plasmaQuery.TryComp(nearbyEntity, out var plasmaComp) || plasmaComp.Plasma == plasmaComp.MaxPlasma)
                        continue;
                    break;
            }

            possibleTargets.Add(nearbyEntity);
        }

        recoveryNode.Comp.NextRecoveryAt = _time.CurTime + recoveryNode.Comp.Cooldown;

        if (possibleTargets.Count == 0)
        {
            Dirty(recoveryNode);
            return;
        }

        var selectedTarget = _random.Pick(possibleTargets);
        var recover = new DoAfterArgs(EntityManager,
            recoveryNode,
            recoveryNode.Comp.Cooldown,
            new RecoveryNodeRecoverDoAfterEvent(),
            recoveryNode,
            selectedTarget)
        {
            BreakOnMove = true,
            MovementThreshold = 0.5f,
            DuplicateCondition = DuplicateConditions.SameEvent,
            TargetEffect = "RMCEffectHealBusy",
        };

        if (_doafter.TryStartDoAfter(recover, out var id))
        {
            recoveryNode.Comp.DoAfter = id;
            Dirty(recoveryNode);
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-recovery-node-heal-target"),
                selectedTarget,
                selectedTarget);
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-recovery-node-heal-other", ("target", selectedTarget)),
                selectedTarget,
                Filter.PvsExcept(selectedTarget),
                true);
        }
    }

    private void OnRecoveryDoAfter(Entity<RecoveryNodeComponent> recoveryNode, ref RecoveryNodeRecoverDoAfterEvent args)
    {
        recoveryNode.Comp.DoAfter = null;
        Dirty(recoveryNode);

        if (args.Handled || args.Cancelled || args.Target == null)
            return;

        switch (recoveryNode.Comp.RecoveryType)
        {
            case RecoveryType.Health:
                _heal.Heal(args.Target.Value, recoveryNode.Comp.RecoveryAmount);
                break;
            case RecoveryType.Plasma:
                _plasma.RegenPlasma(args.Target.Value, recoveryNode.Comp.RecoveryAmount);
                break;
        }
    }
}
