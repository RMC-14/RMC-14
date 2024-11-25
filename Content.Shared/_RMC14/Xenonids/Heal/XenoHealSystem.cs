using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Heal;

public sealed class XenoHealSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";

    private readonly HashSet<Entity<XenoComponent>> _xenos = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoHealComponent, XenoHealActionEvent>(OnXenoHealAction);
        SubscribeLocalEvent<XenoComponent, XenoApplySalveActionEvent>(OnXenoApplySalveAction);
    }

    private void OnXenoHealAction(Entity<XenoHealComponent> ent, ref XenoHealActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_rmcActions.TryUseAction(args.Performer, args.Action))
            return;

        args.Handled = true;

        _xenos.Clear();
        _entityLookup.GetEntitiesInRange(args.Target, ent.Comp.Radius, _xenos);

        if (_xenos.Count == 0)
            return;

        var msg = "We channel our plasma to heal our sisters' wounds around this area.";
        _popup.PopupClient(msg, args.Target, ent, PopupType.Large);

        var time = _timing.CurTime;
        foreach (var xeno in _xenos)
        {
            if (_mobState.IsDead(xeno))
                continue;

            if (!_mobThreshold.TryGetIncapThreshold(xeno, out var threshold) ||
                threshold <= FixedPoint2.Zero)
            {
                continue;
            }

            if (!_hive.FromSameHive(ent.Owner, xeno.Owner))
                continue;

            var heal = EnsureComp<XenoBeingHealedComponent>(xeno);
            XenoHealStack healStack = new();
            healStack.HealAmount = threshold.Value * ent.Comp.Percentage /
                          (ent.Comp.Duration.TotalSeconds * 10) *
                          (ent.Comp.TimeBetweenHeals.TotalSeconds * 10);
            healStack.Charges = (int)(ent.Comp.Duration.TotalSeconds / ent.Comp.TimeBetweenHeals.TotalSeconds);
            heal.HealStacks.Add(healStack);
            heal.TimeBetweenHeals = ent.Comp.TimeBetweenHeals;

            SpawnAttachedTo(ent.Comp.HealEffect, xeno.Owner.ToCoordinates());
        }
    }

    private void OnXenoApplySalveAction(EntityUid ent, XenoComponent comp, XenoApplySalveActionEvent args)
    {

    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var healQuery = EntityQueryEnumerator<XenoBeingHealedComponent>();
        while (healQuery.MoveNext(out var uid, out var heal))
        {
            if (time < heal.NextHealAt)
                continue;
            if (heal.HealStacks.Count == 0 || _mobState.IsDead(uid))
            {
                RemCompDeferred<XenoBeingHealedComponent>(uid);
                continue;
            }

            List<XenoHealStack> finishedStacks = new();

            foreach (var healStack in heal.HealStacks)
            {
                if (healStack.Charges == 0)
                {
                    finishedStacks.Add(healStack);
                    continue;
                }

                heal.NextHealAt = time + heal.TimeBetweenHeals;
                Dirty(uid, heal);

                var damage = _rmcDamageable.DistributeHealing(uid, BruteGroup, healStack.HealAmount);
                var totalHeal = damage.GetTotal();
                var leftover = healStack.HealAmount - totalHeal;
                if (leftover > FixedPoint2.Zero)
                    damage = _rmcDamageable.DistributeHealing(uid, BruteGroup, leftover, damage);

                _damageable.TryChangeDamage(uid, -damage, true);

                healStack.Charges--;

                if (!heal.ParallizeHealing)
                {
                    break;
                }
            }

            foreach (var stack in finishedStacks)
            {
                heal.HealStacks.Remove(stack);
            }
        }
    }
}
