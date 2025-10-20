using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Shields;
using Content.Shared._RMC14.Xenonids.Energy;
using Content.Shared._RMC14.Xenonids.Projectile;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Shared._RMC14.Xenonids.Hedgehog;

public sealed class XenoSpikeShieldSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly XenoProjectileSystem _xenoProjectile = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly XenoShieldSystem _shield = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoSpikeShieldComponent, DamageModifyAfterResistEvent>(OnHedgehogShieldDamage, before: [typeof(XenoShieldSystem)]);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<XenoSpikeShieldComponent, XenoShieldComponent>();
        while (query.MoveNext(out var uid, out var spike, out var shield))
        {
            if (spike.ShieldExpireAt != null && time >= spike.ShieldExpireAt)
            {
                _shield.RemoveShield(uid, XenoShieldSystem.ShieldType.Hedgehog);
            }
        }
    }

    private void OnHedgehogShieldDamage(Entity<XenoSpikeShieldComponent> ent, ref DamageModifyAfterResistEvent args)
    {
        if (!TryComp<XenoShieldComponent>(ent, out var shield) || !shield.Active || shield.Shield != XenoShieldSystem.ShieldType.Hedgehog)
            return;

        // Fire hedgehog spikes when shield is hit by projectiles
        if (HasComp<ProjectileComponent>(args.Tool))
        {
            _xenoProjectile.TryShoot(
                    ent.Owner,
                    new EntityCoordinates(ent, Vector2.UnitX * 2.5f),
                    FixedPoint2.Zero,
                    ent.Comp.Projectile,
                    null,
                    ent.Comp.ProjectileCount,
                    new Angle(2 * Math.PI), // Full circle
                    15f,
                    projectileHitLimit: ent.Comp.ProjectileHitLimit,
                    predicted: false
                );

            _popup.PopupPredicted(Loc.GetString("rmc-spike-shield-hit", ("user", ent)), ent, ent);
        }

        Dirty(ent, ent.Comp);
    }
}
