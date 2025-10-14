using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Projectiles;
using Content.Shared._RMC14.Armor;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Xenonids.Hedgehog;
using Content.Shared._RMC14.Xenonids.Projectile;
using Robust.Shared.Map;
using System.Numerics;
using Content.Shared.Popups;

namespace Content.Shared._RMC14.Shields;

public sealed partial class XenoShieldSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly XenoProjectileSystem _xenoProjectile = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private static readonly ProtoId<DamageTypePrototype> ShieldSoundDamageType = "Piercing";

    public enum ShieldType
    {
        Generic,
        Ravager,
        Hedgehog,
        Vanguard,
        Praetorian,
        Crusher,
        Warden,
        Gardener,
        ShieldPillar,
        CumulativeGeneric
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoShieldComponent, DamageModifyAfterResistEvent>(OnDamage);
    }

    private void OnDamage(Entity<XenoShieldComponent> ent, ref DamageModifyAfterResistEvent args)
    {
        if (!ent.Comp.Active)
            return;

        if (!args.Damage.AnyPositive())
            return;

        ent.Comp.ShieldAmount -= args.Damage.GetTotal();

        if (ent.Comp.ShieldAmount <= 0)
        {
            var usableShield = ent.Comp.ShieldAmount + args.Damage.GetTotal();
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
            RemoveShield(ent, ent.Comp.Shield);
        }
        else
        {
            if (HasComp<ProjectileComponent>(args.Tool) && args.Damage.DamageDict.ContainsKey(ShieldSoundDamageType))
            {
                _audio.PlayPredicted(ent.Comp.ShieldImpact, ent, null);

                // Fire hedgehog spikes when shield is hit
                if (ent.Comp.Shield == ShieldType.Hedgehog &&
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
            }

            args.Damage.ClampMax(0);
        }

        Dirty(ent, ent.Comp);
    }

    public void ApplyShield(EntityUid uid, ShieldType type, FixedPoint2 amount, TimeSpan? duration = null,
        double decay = 0, bool addShield = false, double maxShield = 200)
    {
        var shieldComp = EnsureComp<XenoShieldComponent>(uid);

        if (shieldComp.Active && shieldComp.Shield == type)
        {
            if (addShield)
                shieldComp.ShieldAmount = Math.Min((shieldComp.ShieldAmount + amount).Double(), maxShield);
            else
                shieldComp.ShieldAmount = Math.Max(shieldComp.ShieldAmount.Double(), amount.Double());

            return;
        }

        RemoveShield(uid, shieldComp.Shield);

        shieldComp.Shield = type;
        shieldComp.ShieldAmount = amount;
        shieldComp.Duration = duration;
        shieldComp.DecayPerSecond = decay;

        if (duration != null)
            shieldComp.ShieldDecayAt = _timing.CurTime + duration.Value;

        shieldComp.Active = true;

        Dirty(uid, shieldComp);
    }

    public void RemoveShield(EntityUid uid, ShieldType shieldType)
    {
        if (!TryComp<XenoShieldComponent>(uid, out var shieldComp))
            return;

        if (!shieldComp.Active)
            return;

        if (shieldComp.Shield == shieldType)
        {
            shieldComp.Active = false;
            shieldComp.ShieldAmount = 0;
            Dirty(uid, shieldComp);
            var ev = new RemovedShieldEvent(shieldType);
            RaiseLocalEvent(uid, ref ev);
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        var shieldQuery = EntityQueryEnumerator<XenoShieldComponent>();
        while (shieldQuery.MoveNext(out var uid, out var shield))
        {
            if (shield.Duration == null)
                continue;

            if (time < shield.ShieldDecayAt)
                continue;

            shield.ShieldAmount -= shield.DecayPerSecond * frameTime;

            if (shield.ShieldAmount <= 0)
            {
                RemoveShield(uid, shield.Shield);
                continue;
            }

            Dirty(uid, shield);
        }
    }
}
