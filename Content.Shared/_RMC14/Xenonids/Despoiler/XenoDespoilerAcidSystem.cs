using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Xenonids.Despoiler.Hypertension;
using Content.Shared._RMC14.Xenonids.Stab;
using Content.Shared._RMC14.Xenonids.Projectile.Spit;
using Content.Shared._RMC14.Xenonids.Projectile.Spit.Charge;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._RMC14.Xenonids.Despoiler;

public sealed class XenoDespoilerAcidSystem : EntitySystem
{
    [Dependency] private readonly XenoDespoilerHypertensionSystem _hyper = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly XenoSpitSystem _xenoSpit = default!;

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

    private void OnGetMeleeDamage(Entity<XenoDespoilerSlashOnHitComponent> ent, ref GetMeleeDamageEvent args)
    {
        if (TryGetHyperBurn(ent, out var burn))
            args.Damage += burn;
    }

    private void OnGetTailStabBonusDamage(Entity<XenoDespoilerSlashOnHitComponent> ent, ref RMCGetTailStabBonusDamageEvent args)
    {
        if (TryGetHyperBurn(ent, out var burn))
            args.Damage += burn;
    }

    private void OnDamageTaken(Entity<XenoDespoilerHypertensionComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta is not { } delta)
            return;

        _hyper.AddPoints(ent, (float) delta.GetTotal() * ent.Comp.PointsPerDamageTaken);
    }

    private bool TryGetHyperBurn(Entity<XenoDespoilerSlashOnHitComponent, XenoDespoilerHypertensionComponent?> ent, [NotNullWhen(true)] out DamageSpecifier? burn)
    {
        burn = null!;
        if (!_hyperQuery.Resolve(ent, ref ent.Comp2, false) || ent.Comp2.Stacks <= 0)
            return false;

        var bonus = ent.Comp2.Stacks * ent.Comp2.BonusBurnPerStack;
        if (bonus <= 0)
            return false;

        burn = new DamageSpecifier
        {
            DamageDict =
            {
                [ent.Comp1.DamageType] = FixedPoint2.New(bonus),
            },
        };

        return true;
    }

    private void OnMeleeHit(Entity<XenoDespoilerSlashOnHitComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        if (!_xenoQuery.HasComp(ent) ||
            !_despoilerQuery.TryComp(ent, out var despoilerComp) ||
            !_hyperQuery.TryComp(ent, out var hyper))
        {
            return;
        }

        foreach (var hit in args.HitEntities)
        {
            if (hit == ent.Owner || _xenoQuery.HasComp(hit))
                continue;

            if (hyper != null && hyper.Stacks >= ent.Comp.EnhanceStacksThreshold)
                ApplyAcid(hit, (ent, despoilerComp));

            if (hyper != null && _marineQuery.HasComp(hit))
                _hyper.AddSlashPoints((ent, hyper));
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
    public void ApplyAcid(EntityUid target, Entity<XenoDespoilerComponent?> caster, bool enhance = false)
    {
        if (!_despoilerQuery.Resolve(caster, ref caster.Comp, false))
            return;

        if (_mobState.IsDead(target))
            return;

        if (!_userAcidedQuery.HasComp(target) && caster.Comp.AcidComponents.Count > 0)
            EntityManager.AddComponents(target, caster.Comp.AcidComponents, removeExisting: false);

        var tier = EnsureComp<XenoDespoilerAcidTierComponent>(target);
        if (enhance)
        {
            tier.Tier = tier.MaxTier;

            if (_userAcidedQuery.TryComp(target, out var acid))
                _xenoSpit.EnhanceAcid((target, acid), tier.MaxTier);
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
