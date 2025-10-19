using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Damage.ObstacleSlamming;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stun;
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
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Xenonids.Dislocate;

public sealed class XenoDislocateSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly RMCPullingSystem _rmcPulling = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly SharedRMCMeleeWeaponSystem _rmcMelee = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly RMCSizeStunSystem _sizeStun = default!;
    [Dependency] private readonly RMCObstacleSlammingSystem _obstacleSlamming = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoDislocateComponent, XenoDislocateActionEvent>(OnDislocateAction);
    }

    private void OnDislocateAction(Entity<XenoDislocateComponent> xeno, ref XenoDislocateActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_rmcActions.TryUseAction(args))
            return;

        args.Handled = true;

        if (_net.IsServer)
            _audio.PlayPvs(xeno.Comp.Sound, xeno);

        var targetId = args.Target;
        _rmcPulling.TryStopAllPullsFromAndOn(targetId);

        var isDebuffed = HasComp<RMCSlowdownComponent>(targetId) ||
                         HasComp<RMCSuperSlowdownComponent>(targetId) ||
                         HasComp<RMCRootedComponent>(targetId) ||
                         HasComp<StunnedComponent>(targetId) ||
                         _standing.IsDown(targetId);

        var damage = _damageable.TryChangeDamage(targetId, xeno.Comp.Damage, ignoreResistances: isDebuffed, origin: xeno, tool: xeno);
        if (damage?.GetTotal() > FixedPoint2.Zero)
        {
            var filter = Filter.Pvs(targetId, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == xeno.Owner);
            _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { targetId }, filter);
        }

        if (isDebuffed)
        {
            _slow.TryRoot(targetId, _xeno.TryApplyXenoDebuffMultiplier(targetId, xeno.Comp.RootTime), true);
        }
        else
        {
            if (_net.IsServer)
            {
                var origin = _transform.GetMapCoordinates(xeno);
                _rmcMelee.DoLunge(xeno, targetId);
                _obstacleSlamming.MakeImmune(targetId);
                _sizeStun.KnockBack(targetId, origin, xeno.Comp.FlingRange, xeno.Comp.FlingRange, 10);

                SpawnAttachedTo(xeno.Comp.Effect, targetId.ToCoordinates());
            }
        }

        RefreshCooldowns(xeno);
    }

    private void RefreshCooldowns(Entity<XenoDislocateComponent> xeno)
    {
        foreach (var action in _actions.GetActions(xeno))
        {
            var actionEvent = _actions.GetEvent(action);
            if ((actionEvent is XenoAbductActionEvent || actionEvent is XenoTailLashActionEvent)
                && action.Comp.Cooldown != null)
            {
                var cooldownEnd = action.Comp.Cooldown.Value.End - xeno.Comp.CooldownReductionTime;
                if (cooldownEnd < action.Comp.Cooldown.Value.Start)
                    _actions.ClearCooldown(action.AsNullable());
                else
                    _actions.SetCooldown(action.AsNullable(), action.Comp.Cooldown.Value.Start, cooldownEnd);
            }
        }
    }
}
