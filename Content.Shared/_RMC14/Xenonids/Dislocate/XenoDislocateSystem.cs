using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared._RMC14.Xenonids.Abduct;
using Content.Shared._RMC14.Xenonids.Tail_Lash;
using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using System;

namespace Content.Shared._RMC14.Xenonids.Dislocate;

public sealed partial class XenoDislocateSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly RMCPullingSystem _rmcPulling = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly SharedRMCMeleeWeaponSystem _rmcMelee = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly RMCActionsSystem _rmcActions = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoDislocateComponent, XenoDislocateActionEvent>(OnDislocateAction);
    }

    private void OnDislocateAction(Entity<XenoDislocateComponent> xeno, ref XenoDislocateActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_rmcActions.TryUseAction(xeno, args.Action))
            return;

        args.Handled = true;

        if (_net.IsServer)
            _audio.PlayPvs(xeno.Comp.Sound, xeno);

        var targetId = args.Target;
        _rmcPulling.TryStopAllPullsFromAndOn(targetId);

        var isDebuffed = false;

        if (HasComp<RMCSlowdownComponent>(targetId) || HasComp<RMCSuperSlowdownComponent>(targetId) ||
            HasComp<RMCRootedComponent>(targetId) || HasComp<StunnedComponent>(targetId) ||
            _standing.IsDown(targetId))
        {
            isDebuffed = true;
        }

        var damage = _damageable.TryChangeDamage(targetId, xeno.Comp.Damage, ignoreResistances: isDebuffed);
        if (damage?.GetTotal() > FixedPoint2.Zero)
        {
            var filter = Filter.Pvs(targetId, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == xeno.Owner);
            _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { targetId }, filter);
        }

        if (isDebuffed)
        {
            _slow.TryRoot(targetId, xeno.Comp.RootTime, true);
        }
        else
        {
            if (_net.IsServer)
            {
                var origin = _transform.GetMapCoordinates(xeno);
                var target = _transform.GetMapCoordinates(targetId);
                var diff = target.Position - origin.Position;
                diff = diff.Normalized() * xeno.Comp.FlingRange;

                _rmcMelee.DoLunge(xeno, targetId);

                _throwing.TryThrow(targetId, diff, 10);


                SpawnAttachedTo(xeno.Comp.Effect, targetId.ToCoordinates());
            }
        }

        RefreshCooldowns(xeno);
    }

    private void RefreshCooldowns(Entity<XenoDislocateComponent> xeno)
    {
        foreach (var (actionId, action) in _actions.GetActions(xeno))
        {
            if ((action.BaseEvent is XenoAbductActionEvent || action.BaseEvent is XenoTailLashActionEvent)
                && action.Cooldown != null)
            {
                var cooldownEnd = action.Cooldown.Value.End - xeno.Comp.CooldownReductionTime;
                if (cooldownEnd < action.Cooldown.Value.Start)
                    _actions.ClearCooldown(actionId);
                else
                    _actions.SetCooldown(actionId, action.Cooldown.Value.Start, cooldownEnd);
            }
        }
    }
}
