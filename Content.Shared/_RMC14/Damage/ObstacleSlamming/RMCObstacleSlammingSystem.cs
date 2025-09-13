using System.Numerics;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stun;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Effects;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Damage.ObstacleSlamming;

public sealed class RMCObstacleSlammingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly RMCSizeStunSystem _size = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;

    private static readonly ProtoId<DamageTypePrototype> SlamDamageType = "Blunt";
    private readonly HashSet<EntityUid> _queuedImmuneEntities = new();
    private readonly HashSet<EntityUid> _queuedBonusEntities = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCObstacleSlammingComponent, ThrowDoHitEvent>(HandleCollide);
    }

    private void HandleCollide(Entity<RMCObstacleSlammingComponent> ent, ref ThrowDoHitEvent args)
    {
        var user = args.Thrown;
        var obstacle = args.Target;

        if (args.Handled)
            return;

        if (user != ent.Owner)
            return;

        if (HasComp<RMCObstacleSlamImmuneComponent>(user))
            return;

        if (HasComp<RMCObstacleSlamCauserImmuneComponent>(obstacle))
            return;

        if (!TryComp<PhysicsComponent>(user, out var body) || !TryComp<PhysicsComponent>(obstacle, out var bodyObstacle))
            return;

        if (!body.Hard || !bodyObstacle.Hard)
            return;

        if (!_size.TryGetSize(user, out var size))
            return;

        if (!HasComp<DamageableComponent>(user))
            return;

        var speed = body.LinearVelocity.Length();

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

        if (TryComp<RMCObstacleSlamBonusEffectsComponent>(user, out var bonus))
        {
            _slow.TrySlowdown(user, bonus.Slow, false);
            _stun.TryParalyze(user, bonus.Stun, false);
        }

        // Knockback around 1 tile
        _physics.SetLinearVelocity(ent, Vector2.Zero);
        _physics.SetAngularVelocity(ent, 0f);

        var vec = _transform.GetMoverCoordinates(user).Position - _transform.GetMoverCoordinates(obstacle).Position;
        if (vec.Length() != 0)
        {
            _size.KnockBack(user, _transform.GetMapCoordinates(obstacle), ent.Comp.KnockbackPower, ent.Comp.KnockbackPower, knockBackSpeed: ent.Comp.KnockBackSpeed);
        }

        if (_timing.IsFirstTimePredicted)
            _audio.PlayPvs(ent.Comp.SoundHit, obstacle);

        if (_net.IsServer)
            SpawnAttachedTo(ent.Comp.HitEffect, user.ToCoordinates());

        var selfMessage = Loc.GetString("rmc-obstacle-slam-self", ("ent", user), ("object", Identity.Name(obstacle, EntityManager, user)));
        _popup.PopupClient(selfMessage, user, user, PopupType.MediumCaution);

        var others = Filter.PvsExcept(user).Recipients;
        foreach (var other in others)
        {
            if (other.AttachedEntity is not { } otherEnt)
                continue;

            var otherMessage = Loc.GetString("rmc-obstacle-slam-others", ("ent", user), ("object", Identity.Name(obstacle, EntityManager, otherEnt)));
            _popup.PopupEntity(otherMessage, user, otherEnt, PopupType.MediumCaution);
        }

        args.Handled = true;
    }

    public void MakeImmune(EntityUid uid, float immuneDuration = 2)
    {
        var comp = EnsureComp<RMCObstacleSlamImmuneComponent>(uid);
        comp.ExpireIn = TimeSpan.FromSeconds(immuneDuration);
        comp.ExpireAt = _timing.CurTime + comp.ExpireIn;
        Dirty(uid, comp);
    }

    public void ApplyBonuses(EntityUid uid, TimeSpan stun, TimeSpan slow)
    {
        var comp = EnsureComp<RMCObstacleSlamBonusEffectsComponent>(uid);
        comp.ExpireAt = _timing.CurTime + comp.ExpireIn;
        comp.Stun = stun;
        comp.Slow = slow;
        Dirty(uid, comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _queuedImmuneEntities.Clear();

        var immuneQuery = EntityQueryEnumerator<RMCObstacleSlamImmuneComponent>();
        while (immuneQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.ExpireAt != null && comp.ExpireAt.Value > _timing.CurTime)
                continue;

            _queuedImmuneEntities.Add(uid);
        }

        foreach (var queued in _queuedImmuneEntities)
        {
            RemComp<RMCObstacleSlamImmuneComponent>(queued);
        }

        _queuedBonusEntities.Clear();

        var bonusQuery = EntityQueryEnumerator<RMCObstacleSlamBonusEffectsComponent>();
        while (bonusQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.ExpireAt != null && comp.ExpireAt.Value > _timing.CurTime)
                continue;

            _queuedBonusEntities.Add(uid);
        }

        foreach (var queued in _queuedBonusEntities)
        {
            RemComp<RMCObstacleSlamBonusEffectsComponent>(queued);
        }
    }
}
