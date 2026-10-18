using Content.Shared._RMC14.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Projectiles;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;

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
        if (ent.Comp.Shields.Count == 0)
            return;

        if (!args.Damage.AnyPositive())
            return;

        List<ShieldType> toRemove = new();

        foreach (var shield in ent.Comp.Shields)
        {
            var ev = new XenoShieldHitEvent(shield, args);
            RaiseLocalEvent(ent, ev);

            shield.ShieldAmount -= args.Damage.GetTotal();

            if (shield.ShieldAmount <= 0)
            {
                var usableShield = shield.ShieldAmount + args.Damage.GetTotal();
                shield.ShieldAmount = 0;

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

                    toRemove.Add(shield.Shield);
                }
            }
            else
            {
                if (HasComp<ProjectileComponent>(args.Tool) && args.Damage.DamageDict.ContainsKey(ShieldSoundDamageType))
                {
                    _audio.PlayPredicted(ent.Comp.ShieldImpact, ent, null);
                }

                args.Damage.ClampMax(0);
                UpdateVisuals(ent);
            }

            Dirty(ent, ent.Comp);

            if (args.Damage.GetTotal() <= 0)
                break;
        }

        foreach (var shield in toRemove)
        {
            RemoveShield(ent, shield);
            _audio.PlayPredicted(ent.Comp.ShieldBreak, ent, null);
        }
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
        double decay = 0, bool addShield = false, double maxShield = 200, string? visualState = null, int visualPriority = 0)
    {
        if (HasComp<UnshieldableComponent>(uid))
            return false;

        var shieldComp = EnsureComp<XenoShieldComponent>(uid);

        foreach (var xenoShield in shieldComp.Shields)
        {
            if (xenoShield.Shield == type)
            {
                if (addShield)
                    xenoShield.ShieldAmount = Math.Min((xenoShield.ShieldAmount + amount).Double(), maxShield);
                else
                    xenoShield.ShieldAmount = Math.Max(xenoShield.ShieldAmount.Double(), amount.Double());

                Dirty(uid, shieldComp);
                UpdateVisuals((uid, shieldComp));
                return true;
            }
        }
        var shield = new XenoShield(type, amount, decay, visualState, visualPriority, maxShield);

        if (duration != null)
            shield.ShieldDecayAt = _timing.CurTime + duration.Value;

        shieldComp.Shields.Add(shield);

        UpdateVisuals((uid, shieldComp));

        Dirty(uid, shieldComp);
        return true;
    }

    public void RemoveShield(EntityUid uid, ShieldType shieldType)
    {
        if (!TryComp<XenoShieldComponent>(uid, out var shieldComp))
            return;

        if (shieldComp.Shields.Count == 0)
            return;

        if (TryGetShield(uid, shieldType, out var shield))
        {
            shieldComp.Shields.Remove(shield);
            UpdateVisuals((uid, shieldComp));
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
        while (shieldQuery.MoveNext(out var uid, out var shieldComp))
        {
            List<ShieldType> toRemove = new();
            foreach (var shield in shieldComp.Shields)
            {
                if (shield.ShieldDecayAt == null || time < shield.ShieldDecayAt)
                    continue;

                shield.ShieldAmount -= shield.DecayPerSecond * frameTime;
                UpdateVisuals((uid, shieldComp));

                if (shield.ShieldAmount <= 0)
                {
                    toRemove.Add(shield.Shield);
                    continue;
                }

                Dirty(uid, shieldComp);
            }

            foreach (var shield in toRemove)
            {
                RemoveShield(uid, shield);
            }
        }
    }

    public bool TryGetShield(Entity<XenoShieldComponent?> xeno, ShieldType shieldType, [NotNullWhen(true)] out XenoShield? xenoShield)
    {
        xenoShield = null;
        if (!Resolve(xeno, ref xeno.Comp, false))
            return false;

        foreach (var shield in xeno.Comp.Shields)
        {
            if (shield.Shield == shieldType)
            {
                xenoShield = shield;
                return true;
            }
        }

        return false;
    }

    public FixedPoint2 GetTotalShieldValue(Entity<XenoShieldComponent?> xeno)
    {
        if (!Resolve(xeno, ref xeno.Comp, false))
            return 0;

        FixedPoint2 shieldAmount = 0;

        foreach (var shield in xeno.Comp.Shields)
        {
            shieldAmount += shield.ShieldAmount;
        }

        return shieldAmount;
    }

    public void UpdateVisuals(Entity<XenoShieldComponent> xeno)
    {
        var visualShields = new List<XenoShield>();

        foreach (var shield in xeno.Comp.Shields)
        {
            if (shield.Visual == null)
                continue;

            visualShields.Add(shield);
        }

        if (visualShields.Count == 0)
        {
            _appearance.SetData(xeno, RMCShieldVisuals.Active, false);
            return;
        }

        XenoShield visualShield = visualShields[0];

        if (visualShield.Visual == null)
            return;

        _appearance.SetData(xeno, RMCShieldVisuals.Prefix, visualShield.Visual);
        _appearance.SetData(xeno, RMCShieldVisuals.Active, true);
        _appearance.SetData(xeno, RMCShieldVisuals.Current, visualShield.ShieldAmount);
        _appearance.SetData(xeno, RMCShieldVisuals.Max, visualShield.MaxShield);
    }
}
