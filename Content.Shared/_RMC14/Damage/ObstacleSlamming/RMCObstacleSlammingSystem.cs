using Content.Shared.Effects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Shared.Damage;
using Content.Shared._RMC14.Stun;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Network;
using Content.Shared.Coordinates;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Systems;
using System.Numerics;
using Content.Shared.Popups;

namespace Content.Shared._RMC14.Damage.ObstacleSlamming;

public sealed class RMCObstacleSlammingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly RMCSizeStunSystem _size = default!;

    private static readonly ProtoId<DamageTypePrototype> SlamDamageType = "Blunt";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCObstacleSlammingComponent, StartCollideEvent>(HandleCollide);
    }

    private void HandleCollide(Entity<RMCObstacleSlammingComponent> ent, ref StartCollideEvent args)
    {
        if (!args.OurFixture.Hard || !args.OtherFixture.Hard)
            return;

        var user = args.OurEntity;

        if (!_size.TryGetSize(user, out var size))
            return;

        if (!HasComp<DamageableComponent>(user))
            return;

        var speed = args.OurBody.LinearVelocity.Length();

        if (speed < ent.Comp.MinimumSpeed)
            return;

        if (ent.Comp.LastHit != null && _timing.CurTime - ent.Comp.LastHit.Value < ent.Comp.DamageCooldown)
            return;

        ent.Comp.LastHit = _timing.CurTime;

        var damageAmount = (1 + ent.Comp.MobSizeCoefficient / ((int)size + 1)) * ent.Comp.ThrowSpeedCoefficient * speed;
        var damage = new DamageSpecifier
        {
            DamageDict = { [SlamDamageType] = damageAmount },
        };

        _damageable.TryChangeDamage(user, damage);
        _color.RaiseEffect(Color.Red, new List<EntityUid>() { user }, Filter.Pvs(user));

        // Knockback around 1 tile
        _physics.SetLinearVelocity(ent, Vector2.Zero);
        _physics.SetAngularVelocity(ent, 0f);

        var vec = _transform.GetMoverCoordinates(user).Position - _transform.GetMoverCoordinates(args.OtherEntity).Position;
        if (vec.Length() != 0)
        {
            var direction = vec.Normalized() * ent.Comp.KnockbackPower;
            _throwing.TryThrow(user, direction, ent.Comp.KnockBackSpeed, animated: true, playSound: false, doSpin: false);
        }

        if (_timing.IsFirstTimePredicted)
            _audio.PlayPvs(ent.Comp.SoundHit, user);

        if (_net.IsServer)
            SpawnAttachedTo(ent.Comp.HitEffect, user.ToCoordinates());

        var selfMessage = Loc.GetString("rmc-obstacle-slam-self", ("ent", user), ("object", args.OtherEntity));
        var othersMessage = Loc.GetString("rmc-obstacle-slam-others", ("ent", user), ("object", args.OtherEntity));
        _popup.PopupPredicted(selfMessage, othersMessage, user, user, PopupType.MediumCaution);
    }
}
