using Content.Shared._RMC14.Xenonids.Heal;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Construction.RecoveryNode;

public sealed partial class RecoveryNodeSystem : EntitySystem
{
    [Dependency] private IGameTiming _time = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedXenoHiveSystem _hive = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedXenoHealSystem _heal = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedDoAfterSystem _doafter = default!;
    public override void Initialize()
    {
        base.Initialize();

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
            if (comp.NextHealAt < curTime && comp.HealDoAfter == null)
            {
                TryHealRandomXeno((ent, comp));
            }
        }
    }

    private void TryHealRandomXeno(Entity<RecoveryNodeComponent> recoveryNode)
    {
        var (ent, comp) = recoveryNode;
        var nearbyEntities = _lookup.GetEntitiesInRange(ent, comp.HealRange);
        var possibleTargets = new List<EntityUid>();
        foreach (var nearbyEntity in nearbyEntities)
        {
            if (!_hive.FromSameHive(ent, nearbyEntity) ||
                !HasComp<XenoComponent>(nearbyEntity) ||
                !HasComp<XenoRestingComponent>(nearbyEntity) ||
                !TryComp<DamageableComponent>(nearbyEntity, out var damageComp) ||
                damageComp.TotalDamage <= 0 ||
                !HasComp<MobStateComponent>(nearbyEntity) ||
                _mob.IsDead(nearbyEntity))
            {
                continue;
            }

            possibleTargets.Add(nearbyEntity);
        }

        recoveryNode.Comp.NextHealAt = _time.CurTime + recoveryNode.Comp.HealCooldown;

        if (possibleTargets.Count == 0)
            return;

        var selectedTarget = _random.Pick(possibleTargets);
        var recover = new DoAfterArgs(EntityManager, recoveryNode, recoveryNode.Comp.HealCooldown, new RecoveryNodeRecoverDoAfterEvent(), recoveryNode, selectedTarget)
        {
            BreakOnMove = true,
            MovementThreshold = 0.5f,
            DuplicateCondition = DuplicateConditions.SameEvent,
            TargetEffect = "RMCEffectHealBusy",
        };

        if (_doafter.TryStartDoAfter(recover, out var id))
        {
            recoveryNode.Comp.HealDoAfter = id;
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-recovery-node-heal-target"), selectedTarget, selectedTarget);
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-recovery-node-heal-other", ("target", selectedTarget)), selectedTarget, Filter.PvsExcept(selectedTarget), true);
        }
    }

    private void OnRecoveryDoAfter(Entity<RecoveryNodeComponent> recoveryNode, ref RecoveryNodeRecoverDoAfterEvent args)
    {
        recoveryNode.Comp.HealDoAfter = null;

        if (args.Handled || args.Cancelled || args.Target == null)
            return;

        _heal.Heal(args.Target.Value, recoveryNode.Comp.HealAmount);
    }
}
