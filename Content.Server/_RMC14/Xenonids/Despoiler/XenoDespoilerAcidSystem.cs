using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Stab;
using Content.Shared._RMC14.Xenonids.Despoiler;
using Content.Shared._RMC14.Xenonids.Projectile.Spit;
using Content.Shared._RMC14.Xenonids.Projectile.Spit.Charge;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server._RMC14.Xenonids.Despoiler;

public sealed class XenoDespoilerAcidSystem : SharedXenoDespoilerAcidSystem
{
    [Dependency] private readonly XenoDespoilerHypertensionSystem _hyper = default!;
    [Dependency] private readonly XenoSpitSystem _xenoSpit = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private EntityQuery<XenoComponent> _xenoQuery;
    private EntityQuery<XenoDespoilerComponent> _despoilerQuery;
    private EntityQuery<XenoDespoilerHypertensionComponent> _hyperQuery;
    private EntityQuery<MarineComponent> _marineQuery;
    private EntityQuery<UserAcidedComponent> _userAcidedQuery;
    private EntityQuery<XenoDespoilerAcidTierComponent> _tierQuery;

    public override void Initialize()
    {
        _xenoQuery = GetEntityQuery<XenoComponent>();
        _despoilerQuery = GetEntityQuery<XenoDespoilerComponent>();
        _hyperQuery = GetEntityQuery<XenoDespoilerHypertensionComponent>();
        _marineQuery = GetEntityQuery<MarineComponent>();
        _userAcidedQuery = GetEntityQuery<UserAcidedComponent>();
        _tierQuery = GetEntityQuery<XenoDespoilerAcidTierComponent>();

        SubscribeLocalEvent<XenoDespoilerSlashOnHitComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<XenoDespoilerSlashOnHitComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        SubscribeLocalEvent<XenoDespoilerSlashOnHitComponent, RMCGetTailStabBonusDamageEvent>(OnGetTailStabBonusDamage);
        SubscribeLocalEvent<XenoDespoilerHypertensionComponent, DamageChangedEvent>(OnDamageTaken);
    }

    private void OnGetMeleeDamage(EntityUid uid, XenoDespoilerSlashOnHitComponent comp, ref GetMeleeDamageEvent args)
    {
        if (TryGetHyperBurn(uid, out var burn))
            args.Damage += burn;
    }

    private void OnGetTailStabBonusDamage(EntityUid uid, XenoDespoilerSlashOnHitComponent comp, ref RMCGetTailStabBonusDamageEvent args)
    {
        if (TryGetHyperBurn(uid, out var burn))
            args.Damage += burn;
    }

    private void OnDamageTaken(EntityUid uid, XenoDespoilerHypertensionComponent comp, DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta is not { } delta)
            return;

        _hyper.AddPoints(uid, comp, (float) delta.GetTotal() * comp.PointsPerDamageTaken);
    }

    private bool TryGetHyperBurn(EntityUid uid, out DamageSpecifier burn)
    {
        burn = default!;
        if (!_hyperQuery.TryComp(uid, out var hyper) || hyper.Stacks <= 0)
            return false;

        var bonus = hyper.Stacks * hyper.BonusBurnPerStack;
        if (bonus <= 0)
            return false;

        burn = new DamageSpecifier();
        burn.DamageDict["Heat"] = FixedPoint2.New(bonus);
        return true;
    }

    private void OnMeleeHit(EntityUid uid, XenoDespoilerSlashOnHitComponent comp, MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        if (!_xenoQuery.HasComp(uid))
            return;

        _hyperQuery.TryComp(uid, out var hyper);

        foreach (var hit in args.HitEntities)
        {
            if (hit == uid || _xenoQuery.HasComp(hit))
                continue;

            if (hyper != null && hyper.Stacks >= comp.EnhanceStacksThreshold)
                ApplyAcid(hit, uid);

            if (hyper != null && _marineQuery.HasComp(hit))
                _hyper.AddSlashPoints(uid, hyper);
        }
    }

    /// <summary>
    /// Apply Despoiler's lingering acid to <paramref name="target"/> from
    /// <paramref name="caster"/>. Uses the caster's <see cref="XenoDespoilerComponent.AcidComponents"/>
    /// registry to spawn <see cref="UserAcidedComponent"/> on first hit; ticks/visuals
    /// after that are owned by <see cref="XenoSpitSystem"/>. Increments the Despoiler
    /// acid tier on the target; <paramref name="enhance"/> jumps tier to max and flips
    /// the UserAcided combo flag.
    /// </summary>
    public void ApplyAcid(EntityUid target, EntityUid caster, bool enhance = false)
    {
        if (!_despoilerQuery.TryComp(caster, out var despoilerComp))
            return;

        if (_mobState.IsDead(target))
            return;

        if (!_userAcidedQuery.HasComp(target) && despoilerComp.AcidComponents.Count > 0)
            EntityManager.AddComponents(target, despoilerComp.AcidComponents, removeExisting: false);

        var tier = EnsureComp<XenoDespoilerAcidTierComponent>(target);
        if (enhance)
        {
            tier.Tier = tier.MaxTier;
            if (_userAcidedQuery.TryComp(target, out var acid))
            {
                _xenoSpit.SetAcidCombo((target, acid),
                    duration: default, damage: null, paralyze: default, resists: default);
            }
        }
        else if (tier.Tier < tier.MaxTier)
        {
            tier.Tier++;
        }
        Dirty(target, tier);
    }

    /// <summary>
    /// Decrement the Despoiler acid tier on <paramref name="target"/> by one.
    /// Returns the tier value <em>before</em> the decrement, or 0 if no tier
    /// component was present.
    /// </summary>
    public int ConsumeAcidTier(EntityUid target)
    {
        if (!_tierQuery.TryComp(target, out var tier) || tier.Tier <= 0)
            return 0;

        var previous = tier.Tier;
        tier.Tier--;
        if (tier.Tier <= 0)
            RemComp<XenoDespoilerAcidTierComponent>(target);
        else
            Dirty(target, tier);

        return previous;
    }
}
