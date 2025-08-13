using System.Numerics;
using Content.Shared._RMC14.Damage.ObstacleSlamming;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Tumble;

public sealed class XenoTumbleSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCObstacleSlammingSystem _rmcObstacleSlamming = default!;
    [Dependency] private readonly RMCPullingSystem _rmcPulling = default!;
    [Dependency] private readonly RMCSizeStunSystem _sizeStun = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly ThrownItemSystem _thrownItem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<ThrownItemComponent> _thrownItemQuery;

    public override void Initialize()
    {
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _thrownItemQuery = GetEntityQuery<ThrownItemComponent>();

        SubscribeLocalEvent<XenoTumbleComponent, XenoTumbleActionEvent>(OnXenoTumbleAction);
        SubscribeLocalEvent<XenoTumbleComponent, ThrowDoHitEvent>(OnXenoTumbleHit);
        SubscribeLocalEvent<XenoTumbleComponent, LandEvent>(OnXenoTumbleLand);
    }

    private void OnXenoTumbleAction(Entity<XenoTumbleComponent> xeno, ref XenoTumbleActionEvent args)
    {
        if (args.Handled)
            return;

        var origin = _transform.GetMapCoordinates(xeno);
        var target = _transform.ToMapCoordinates(args.Target);
        var diff = target.Position - origin.Position;
        diff = diff.Normalized() * xeno.Comp.Range;
        var dir = diff.GetDir();
        var perpendiculars = _transform.GetWorldRotation(xeno).GetCardinalDir().GetPerpendiculars();
        Direction towards;
        if (dir == perpendiculars.First)
        {
            towards = perpendiculars.First;
        }
        else if (dir == perpendiculars.Second)
        {
            towards = perpendiculars.Second;
        }
        else
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-tumble-not-perpendicular"), args.Target, xeno, PopupType.LargeCaution);
            return;
        }

        diff = towards.ToVec() * xeno.Comp.Range;
        args.Handled = true;

        _rmcPulling.TryStopAllPullsFromAndOn(xeno);

        xeno.Comp.Target = diff;
        Dirty(xeno);

        _rmcObstacleSlamming.MakeImmune(xeno);
        _throwing.TryThrow(xeno, diff, 30, animated: false);
        _audio.PlayPredicted(xeno.Comp.Sound, xeno, xeno);
    }

    private void OnXenoTumbleHit(Entity<XenoTumbleComponent> xeno, ref ThrowDoHitEvent args)
    {
        if (!_mobState.IsAlive(xeno) || HasComp<StunnedComponent>(xeno))
        {
            xeno.Comp.Target = null;
            Dirty(xeno);
            return;
        }

        // TODO RMC14 lag compensation
        if (_mobState.IsDead(args.Target))
            return;

        if (_physicsQuery.TryGetComponent(xeno, out var physics) &&
            _thrownItemQuery.TryGetComponent(xeno, out var thrown))
        {
            _thrownItem.LandComponent(xeno, thrown, physics, true);
            _thrownItem.StopThrow(xeno, thrown);
        }

        if (_timing.IsFirstTimePredicted && xeno.Comp.Target != null)
            xeno.Comp.Target = null;

        if (_hive.FromSameHive(xeno.Owner, args.Target))
            return;

        if (_net.IsServer)
            _stun.TryParalyze(args.Target, xeno.Comp.StunTime, true);

        StopTumble(xeno);

        var origin = _transform.GetMapCoordinates(xeno);
        _sizeStun.KnockBack(args.Target, origin, xeno.Comp.ImpactRange, xeno.Comp.ImpactRange, 10);

        _damageable.TryChangeDamage(
            args.Target,
            _xeno.TryApplyXenoSlashDamageMultiplier(args.Target, xeno.Comp.Damage),
            origin: xeno,
            tool: xeno,
            armorPiercing: xeno.Comp.ArmorPiercing
        );
    }

    private void OnXenoTumbleLand(Entity<XenoTumbleComponent> xeno, ref LandEvent args)
    {
        if (xeno.Comp.Target == null)
            return;

        xeno.Comp.Target = null;
        Dirty(xeno);
    }

    private void StopTumble(EntityUid xeno)
    {
        if (_physicsQuery.TryGetComponent(xeno, out var physics))
        {
            _physics.SetLinearVelocity(xeno, Vector2.Zero, body: physics);
            _physics.SetBodyStatus(xeno, physics, BodyStatus.OnGround);
        }
    }
}
