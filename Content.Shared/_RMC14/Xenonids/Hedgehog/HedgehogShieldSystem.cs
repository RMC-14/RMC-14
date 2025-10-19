using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Xenonids.Projectile;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Shared._RMC14.Xenonids.Hedgehog;

public sealed class HedgehogShieldSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly XenoProjectileSystem _xenoProjectile = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HedgehogShieldComponent, DamageModifyAfterResistEvent>(OnHedgehogShieldDamage);
    }

    private void OnHedgehogShieldDamage(Entity<HedgehogShieldComponent> ent, ref DamageModifyAfterResistEvent args)
    {
        if (!args.Damage.AnyPositive() || ent.Comp.ShieldAmount <= 0)
            return;

        var totalDamage = args.Damage.GetTotal();
        ent.Comp.ShieldAmount -= totalDamage;

        if (ent.Comp.ShieldAmount <= 0)
        {
            var usableShield = ent.Comp.ShieldAmount + totalDamage;
            ent.Comp.ShieldAmount = 0;

            foreach (var type in args.Damage.DamageDict)
            {
                if (usableShield == 0)
                    break;

                if (type.Value > 0)
                {
                    var tempVal = Math.Min(type.Value.Double(), usableShield.Double());
                    args.Damage.DamageDict[type.Key] -= tempVal;
                    usableShield -= tempVal;
                }
            }

            _audio.PlayPredicted(ent.Comp.ShieldBreak, ent, null);
            _popup.PopupEntity("The shield breaks!", ent, ent, PopupType.MediumCaution);
            ent.Comp.Active = false;
            RemCompDeferred<HedgehogShieldComponent>(ent);
        }
        else
        {
            // Fire hedgehog spikes when shield is hit by projectiles
            if (HasComp<ProjectileComponent>(args.Tool) && 
                TryComp<XenoSpikeShieldComponent>(ent, out var spikeShield) &&
                spikeShield.Active)
            {
                _xenoProjectile.TryShoot(
                    ent.Owner,
                    new EntityCoordinates(ent, Vector2.UnitX * 2.5f),
                    FixedPoint2.Zero,
                    spikeShield.Projectile,
                    null,
                    spikeShield.ProjectileCount,
                    new Angle(2 * Math.PI), // Full circle
                    15f,
                    projectileHitLimit: spikeShield.ProjectileHitLimit,
                    predicted: false
                );

                _popup.PopupPredicted("Damaging the shield sprays bone quills everywhere!", ent, ent);
            }

            args.Damage.ClampMax(0);
        }

        Dirty(ent, ent.Comp);
    }

    public void ApplyShield(EntityUid uid, FixedPoint2 amount, TimeSpan duration, double decay = 0)
    {
        var shield = EnsureComp<HedgehogShieldComponent>(uid);
        shield.ShieldAmount = amount;
        shield.EndAt = _timing.CurTime + duration;
        shield.DecayPerSecond = decay;
        shield.Active = true;
        Dirty(uid, shield);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<HedgehogShieldComponent>();

        while (query.MoveNext(out var uid, out var shield))
        {
            if (time >= shield.EndAt)
            {
                _popup.PopupEntity("The shield fades away.", uid, uid, PopupType.MediumCaution);
                shield.Active = false;
                RemCompDeferred<HedgehogShieldComponent>(uid);
                continue;
            }

            if (shield.DecayPerSecond > 0)
            {
                shield.ShieldAmount -= shield.DecayPerSecond * frameTime;
                if (shield.ShieldAmount <= 0)
                {
                    _popup.PopupEntity("The shield decays away.", uid, uid, PopupType.MediumCaution);
                    shield.Active = false;
                    RemCompDeferred<HedgehogShieldComponent>(uid);
                    continue;
                }
                Dirty(uid, shield);
            }
        }
    }
}