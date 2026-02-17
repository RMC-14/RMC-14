using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Projectiles;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Content.Shared._RMC14.Damage;

namespace Content.Shared._RMC14.Shields;

public sealed partial class XenoShieldSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

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
        King,
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
            }

            args.Damage.ClampMax(0);
            _appearance.SetData(ent, RMCShieldVisuals.Current, ent.Comp.ShieldAmount);
        }

        Dirty(ent, ent.Comp);
    }

    /// <summary>
    /// Returns true if shield was applied successfully
    /// </summary>
    /// <param name="uid"> Entity getting shield</param>
    /// <param name="type"> Identifier of the type of shield</param>
    /// <param name="amount"> How much shield</param>
    /// <param name="duration"> How long the shield lasts before decay starts</param>
    /// <param name="decay"> Shield lost per decay tick</param>
    /// <param name="addShield"> Does this add ontop existing shield of the same type?</param>
    /// <param name="maxShield"> Max amount of shield that can be added</param>
    /// <param name="visualState"> If this shield has a special visual overlay</param>
    /// <returns></returns>
    public bool ApplyShield(EntityUid uid, ShieldType type, FixedPoint2 amount, TimeSpan? duration = null,
        double decay = 0, bool addShield = false, double maxShield = 200, string? visualState = null)
    {
        if (HasComp<UnshieldableComponent>(uid))
            return false;

        var shieldComp = EnsureComp<XenoShieldComponent>(uid);

        if (shieldComp.Active && shieldComp.Shield == type)
        {
            if (addShield)
                shieldComp.ShieldAmount = Math.Min((shieldComp.ShieldAmount + amount).Double(), maxShield);
            else
                shieldComp.ShieldAmount = Math.Max(shieldComp.ShieldAmount.Double(), amount.Double());

            _appearance.SetData(uid, RMCShieldVisuals.Current, shieldComp.ShieldAmount);
            return true;
        }

        RemoveShield(uid, shieldComp.Shield);

        shieldComp.Shield = type;
        shieldComp.ShieldAmount = amount;
        shieldComp.Duration = duration;
        shieldComp.DecayPerSecond = decay;

        if (duration != null)
            shieldComp.ShieldDecayAt = _timing.CurTime + duration.Value;

        shieldComp.Active = true;
        if (visualState != null)
        {
            _appearance.SetData(uid, RMCShieldVisuals.Prefix, visualState);
            _appearance.SetData(uid, RMCShieldVisuals.Active, true);
            _appearance.SetData(uid, RMCShieldVisuals.Current, shieldComp.ShieldAmount);
            _appearance.SetData(uid, RMCShieldVisuals.Max, maxShield);
        }

        Dirty(uid, shieldComp);
        return true;
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
            _appearance.SetData(uid, RMCShieldVisuals.Active, false);
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
            _appearance.SetData(uid, RMCShieldVisuals.Current, shield.ShieldAmount);

            if (shield.ShieldAmount <= 0)
            {
                RemoveShield(uid, shield.Shield);
                continue;
            }

            Dirty(uid, shield);
        }
    }
}
