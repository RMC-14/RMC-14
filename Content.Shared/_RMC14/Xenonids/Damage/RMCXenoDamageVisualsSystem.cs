using Content.Shared._RMC14.Xenonids.Fortify;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Rounding;
using Content.Shared.Stunnable;
using Content.Shared.StatusEffect;

namespace Content.Shared._RMC14.Xenonids.Damage;

public sealed class RMCXenoDamageVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;

    private EntityQuery<MobThresholdsComponent> _mobThresholdsQuery;

    public override void Initialize()
    {
        _mobThresholdsQuery = GetEntityQuery<MobThresholdsComponent>();

        SubscribeLocalEvent<RMCXenoDamageVisualsComponent, MobStateChangedEvent>(OnVisualsMobStateChanged);
        SubscribeLocalEvent<RMCXenoDamageVisualsComponent, XenoFortifiedEvent>(OnVisualsFortified);
        SubscribeLocalEvent<RMCXenoDamageVisualsComponent, XenoRestEvent>(OnVisualsRest);
        SubscribeLocalEvent<RMCXenoDamageVisualsComponent, DamageChangedEvent>(OnVisualsDamageChanged);
        SubscribeLocalEvent<RMCXenoDamageVisualsComponent, KnockedDownEvent>(OnVisualsKnockedDown);
        SubscribeLocalEvent<RMCXenoDamageVisualsComponent, StatusEffectEndedEvent>(OnVisualsStatusEffectEnded);
    }

    private void OnVisualsMobStateChanged(Entity<RMCXenoDamageVisualsComponent> ent, ref MobStateChangedEvent args)
    {
        _appearance.SetData(ent, RMCDamageVisuals.Downed, args.NewMobState != MobState.Alive);
    }

    private void OnVisualsFortified(Entity<RMCXenoDamageVisualsComponent> ent, ref XenoFortifiedEvent args)
    {
        _appearance.SetData(ent, RMCDamageVisuals.Fortified, args.Fortified);
    }

    private void OnVisualsRest(Entity<RMCXenoDamageVisualsComponent> ent, ref XenoRestEvent args)
    {
        _appearance.SetData(ent, RMCDamageVisuals.Resting, args.Resting);
    }

    private void OnVisualsKnockedDown(Entity<RMCXenoDamageVisualsComponent> xeno, ref KnockedDownEvent args)
    {
        _appearance.SetData(xeno, RMCDamageVisuals.Downed, true);
    }

    private void OnVisualsStatusEffectEnded(Entity<RMCXenoDamageVisualsComponent> xeno, ref StatusEffectEndedEvent args)
    {
        if (args.Key == "KnockedDown")
            _appearance.SetData(xeno, RMCDamageVisuals.Downed, false);
    }

    private void OnVisualsDamageChanged(Entity<RMCXenoDamageVisualsComponent> ent, ref DamageChangedEvent args)
    {
        if (!_mobThresholdsQuery.TryComp(ent, out var thresholds) ||
            !_thresholds.TryGetIncapThreshold(ent, out var threshold, thresholds))
        {
            return;
        }

        var damage = args.Damageable.TotalDamage.Double();
        var max = threshold.Value.Double();
        var level = ContentHelpers.RoundToEqualLevels(damage, max, ent.Comp.States + 1);
        _appearance.SetData(ent, RMCDamageVisuals.State, level);
    }
}
