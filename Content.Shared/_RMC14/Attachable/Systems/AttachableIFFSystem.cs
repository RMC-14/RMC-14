using System.Collections.Generic;
using System.Numerics;
using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared._RMC14.Random;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Projectiles;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Physics;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed class AttachableIFFSystem : EntitySystem
{
    [Dependency] private readonly AttachableHolderSystem _holder = default!;
    [Dependency] private readonly GunIFFSystem _gunIFF = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const float FallbackProjectileLifetime = 10f;
    private const float FallbackProjectileRadius = 0.1f;

    private readonly HashSet<EntProtoId<IFFFactionComponent>> _factionBuffer = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableIFFComponent, AttachableAlteredEvent>(OnAttachableIFFAltered);
        SubscribeLocalEvent<AttachableIFFComponent, AttachableRelayedEvent<AttachableGrantIFFEvent>>(OnAttachableIFFGrant);

        SubscribeLocalEvent<GunAttachableIFFComponent, AttemptShootEvent>(OnGunAttachableIFFAttemptShoot);
        SubscribeLocalEvent<GunAttachableIFFComponent, AmmoShotEvent>(OnGunAttachableIFFAmmoShot);
        SubscribeLocalEvent<GunAttachableIFFComponent, ExaminedEvent>(OnGunAttachableIFFExamined);
    }

    private void OnAttachableIFFAltered(Entity<AttachableIFFComponent> ent, ref AttachableAlteredEvent args)
    {
        switch (args.Alteration)
        {
            case AttachableAlteredType.Attached:
                UpdateGunIFF(args.Holder);
                break;
            case AttachableAlteredType.Detached:
                UpdateGunIFF(args.Holder);
                break;
        }
    }

    private void OnAttachableIFFGrant(Entity<AttachableIFFComponent> ent, ref AttachableRelayedEvent<AttachableGrantIFFEvent> args)
    {
        args.Args.Grants = true;

        if (ent.Comp.PreventFriendlyFire)
            args.Args.PreventFriendlyFire = true;
    }

    private void OnGunAttachableIFFAttemptShoot(Entity<GunAttachableIFFComponent> ent, ref AttemptShootEvent args)
    {
        if (args.Cancelled ||
            !ent.Comp.PreventFriendlyFire ||
            args.ToCoordinates is not { } toCoordinates ||
            !TryComp<GunComponent>(ent, out var gun))
        {
            return;
        }

        if (!_gunIFF.TryGetFactions((args.User, CompOrNull<UserIFFComponent>(args.User)), _factionBuffer, SlotFlags.IDCARD))
            return;

        try
        {
            var from = _transform.ToMapCoordinates(args.FromCoordinates);
            var to = _transform.ToMapCoordinates(toCoordinates);
            if (from.MapId != to.MapId)
                return;

            var initialDirection = to.Position - from.Position;
            if (initialDirection == Vector2.Zero)
                return;

            var shotDirection = GetPredictedShotDirection((ent.Owner, gun), initialDirection);
            if (shotDirection == Vector2.Zero)
                return;

            var maxDistance = GetIffCheckDistance((ent.Owner, gun));
            if (maxDistance <= 0)
                return;

            var shotEnd = from.Position + shotDirection * maxDistance;
            var shotRadius = GetIffCheckRadius((ent.Owner, gun));
            var perpendicular = new Vector2(-shotDirection.Y, shotDirection.X);
            var shooter = args.User;
            var weapon = ent.Owner;

            var blockedByFriendly = false;
            var hasHit = false;
            var hitPosition = shotEnd;
            blockedByFriendly = ProcessRay(0f);
            if (!blockedByFriendly && shotRadius > 0f)
            {
                blockedByFriendly = ProcessRay(shotRadius) || ProcessRay(-shotRadius);
            }

            if (blockedByFriendly)
            {
                args.Cancelled = true;
                args.Message = Loc.GetString("rmc-iff-friendly-in-line");
            }

            RaiseDebugSample(args.User, from, shotEnd, hasHit, hitPosition, blockedByFriendly);

            bool ProcessRay(float offset)
            {
                var ray = new CollisionRay(
                    from.Position + perpendicular * offset,
                    shotDirection,
                    (int) CollisionGroup.BulletImpassable);
                var hits = _physics.IntersectRayWithPredicate(
                    from.MapId,
                    ray,
                    (user: shooter, gun: weapon),
                    static (uid, state) => uid == state.user || uid == state.gun,
                    maxDistance,
                    returnOnFirstHit: false);

                foreach (var hit in hits)
                {
                    hasHit = true;
                    hitPosition = hit.HitPos;

                    if (IsFriendlyEntity(hit.HitEntity))
                    {
                        return true;
                    }

                    break;
                }

                return false;
            }
        }
        finally
        {
            _factionBuffer.Clear();
        }
    }

    private Vector2 GetPredictedShotDirection(Entity<GunComponent> gun, Vector2 initialDirection)
    {
        var baseAngle = initialDirection.ToAngle();

        var timeSinceLastFire = (_timing.CurTime - gun.Comp.LastFire).TotalSeconds;
        var newTheta = MathHelper.Clamp(
            gun.Comp.CurrentAngle.Theta + gun.Comp.AngleIncreaseModified.Theta - gun.Comp.AngleDecayModified.Theta * timeSinceLastFire,
            gun.Comp.MinAngleModified.Theta,
            gun.Comp.MaxAngleModified.Theta);

        long tick = _timing.CurTick.Value;
        tick = tick << 32;
        tick |= (uint) GetNetEntity(gun.Owner).Id;
        var random = new Xoroshiro64S(tick).NextFloat(-0.5f, 0.5f);

        var shotAngle = new Angle(baseAngle.Theta + newTheta * random);
        return shotAngle.ToVec();
    }

    private bool IsFriendlyEntity(EntityUid target)
    {
        foreach (var faction in _factionBuffer)
        {
            if (_gunIFF.IsInFaction(target, faction))
                return true;
        }

        return false;
    }

    private float GetIffCheckDistance(Entity<GunComponent> gun)
    {
        return gun.Comp.ProjectileSpeedModified * FallbackProjectileLifetime;
    }

    private float GetIffCheckRadius(Entity<GunComponent> _)
    {
        return FallbackProjectileRadius;
    }

    private void RaiseDebugSample(
        EntityUid user,
        MapCoordinates from,
        Vector2 shotEnd,
        bool hasHit,
        Vector2 hitPosition,
        bool blockedByFriendly)
    {
        if (_net.IsClient && !_timing.IsFirstTimePredicted)
            return;

        var ev = new AttachableIFFDebugSampleEvent(
            user,
            from,
            new MapCoordinates(shotEnd, from.MapId),
            hasHit,
            new MapCoordinates(hitPosition, from.MapId),
            blockedByFriendly,
            _net.IsServer);
        RaiseLocalEvent(ref ev);
    }

    private void OnGunAttachableIFFAmmoShot(Entity<GunAttachableIFFComponent> ent, ref AmmoShotEvent args)
    {
        _gunIFF.GiveAmmoIFF(ent, ref args, false, true);
    }

    private void OnGunAttachableIFFExamined(Entity<GunAttachableIFFComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(GunAttachableIFFComponent)))
        {
            args.PushMarkup(Loc.GetString(ent.Comp.PreventFriendlyFire
                ? "rmc-examine-text-iff-prevent-friendly-fire"
                : "rmc-examine-text-iff"));
        }
    }

    private void UpdateGunIFF(EntityUid gun)
    {
        if (!TryComp(gun, out AttachableHolderComponent? holder))
            return;

        var ev = new AttachableGrantIFFEvent();
        _holder.RelayEvent((gun, holder), ref ev);

        if (_timing.ApplyingState)
            return;

        if (ev.Grants)
        {
            var gunIff = EnsureComp<GunAttachableIFFComponent>(gun);
            if (gunIff.PreventFriendlyFire != ev.PreventFriendlyFire)
            {
                gunIff.PreventFriendlyFire = ev.PreventFriendlyFire;
                Dirty(gun, gunIff);
            }
        }
        else
        {
            RemCompDeferred<GunAttachableIFFComponent>(gun);
        }
    }
}
