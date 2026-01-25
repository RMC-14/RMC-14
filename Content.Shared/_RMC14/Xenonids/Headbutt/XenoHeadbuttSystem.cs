using System.Numerics;
using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Damage.ObstacleSlamming;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids.Animation;
using Content.Shared._RMC14.Xenonids.Crest;
using Content.Shared._RMC14.Xenonids.Fortify;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Headbutt;

public sealed class XenoHeadbuttSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly RMCObstacleSlammingSystem _rmcObstacleSlamming = default!;
    [Dependency] private readonly RMCPullingSystem _rmcPulling = default!;
    [Dependency] private readonly RMCSizeStunSystem _sizeStun = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ThrownItemSystem _thrownItem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoAnimationsSystem _xenoAnimations = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<ThrownItemComponent> _thrownItemQuery;

    public override void Initialize()
    {
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _thrownItemQuery = GetEntityQuery<ThrownItemComponent>();

        SubscribeLocalEvent<XenoHeadbuttComponent, XenoHeadbuttActionEvent>(OnXenoHeadbuttAction);
        SubscribeLocalEvent<XenoHeadbuttComponent, ThrowDoHitEvent>(OnXenoHeadbuttHit);
    }

    private void OnXenoHeadbuttAction(Entity<XenoHeadbuttComponent> xeno, ref XenoHeadbuttActionEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<XenoCrestComponent>(xeno, out var crest) && crest.Lowered && !_interaction.InRangeUnobstructed(xeno.Owner, args.Target))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-headbutt-too-far"), xeno, xeno, PopupType.SmallCaution);
            return;
        }

        var attempt = new XenoHeadbuttAttemptEvent();
        RaiseLocalEvent(xeno, ref attempt);

        if (attempt.Cancelled)
            return;

        if (!_rmcActions.TryUseAction(args))
            return;

        _rmcPulling.TryStopAllPullsFromAndOn(xeno);

        args.Handled = true;
        var origin = _transform.GetMapCoordinates(xeno);
        var target = _transform.GetMapCoordinates(args.Target);
        var diff = target.Position - origin.Position;
        diff = diff.Normalized() * xeno.Comp.Range;

        xeno.Comp.Charge = diff;
        Dirty(xeno);

        _rmcObstacleSlamming.MakeImmune(xeno);
        _throwing.TryThrow(xeno, diff);
    }

    private void OnXenoHeadbuttHit(Entity<XenoHeadbuttComponent> xeno, ref ThrowDoHitEvent args)
    {
        // TODO RMC14 lag compensation
        var targetId = args.Target;
        if (!_xeno.CanAbilityAttackTarget(xeno, targetId))
            return;

        if (_physicsQuery.TryGetComponent(xeno, out var physics) &&
            _thrownItemQuery.TryGetComponent(xeno, out var thrown))
        {
            _thrownItem.LandComponent(xeno, thrown, physics, true);
            _thrownItem.StopThrow(xeno, thrown);
        }

        if (_timing.IsFirstTimePredicted && xeno.Comp.Charge is { } charge)
        {
            xeno.Comp.Charge = null;
            _xenoAnimations.PlayLungeAnimationEvent(xeno, charge);
        }

        if (_net.IsServer)
            _audio.PlayPvs(xeno.Comp.Sound, xeno);

        var finalDamage = xeno.Comp.Damage;

        if (TryComp<XenoCrestComponent>(xeno, out var crest) && crest.Lowered)
        {
            finalDamage.ExclusiveAdd(xeno.Comp.CrestedDamageReduction);
        }

        var damage = _damageable.TryChangeDamage(targetId, _xeno.TryApplyXenoSlashDamageMultiplier(targetId, finalDamage), armorPiercing: xeno.Comp.AP, origin: xeno, tool: xeno);
        if (damage?.GetTotal() > FixedPoint2.Zero)
        {
            var filter = Filter.Pvs(targetId, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == xeno.Owner);
            _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { targetId }, filter);
        }

        var range = xeno.Comp.ThrowForce +
           ((TryComp<XenoCrestComponent>(xeno, out var crest2) && crest2.Lowered) || (TryComp<XenoFortifyComponent>(xeno, out var fort) && fort.Fortified) ? xeno.Comp.CrestFortifiedThrowAdd : 0);
        _rmcPulling.TryStopAllPullsFromAndOn(targetId);

        StopHeadbutt(xeno);

        var origin = _transform.GetMapCoordinates(xeno);
        _sizeStun.KnockBack(targetId, origin, range, range, 10, true );

        if (_net.IsServer)
            SpawnAttachedTo(xeno.Comp.Effect, targetId.ToCoordinates());
    }

    private void StopHeadbutt(EntityUid xeno)
    {
        if (_physicsQuery.TryGetComponent(xeno, out var physics))
        {
            _physics.SetLinearVelocity(xeno, Vector2.Zero, body: physics);
            _physics.SetBodyStatus(xeno, physics, BodyStatus.OnGround);
        }
    }
}
