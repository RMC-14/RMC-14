using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Rounding;

namespace Content.Shared._RMC14.Xenonids.Damage;

public sealed class RMCXenoDamageVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;

    private EntityQuery<MobThresholdsComponent> _mobThresholdsQuery;

    public override void Initialize()
    {
        _mobThresholdsQuery = GetEntityQuery<MobThresholdsComponent>();

        SubscribeLocalEvent<RMCXenoDamageVisualsComponent, DamageChangedEvent>(OnVisualsDamageChanged);
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
        int level;
        if (damage > threshold)
            level = ent.Comp.States + 1;
        else
            level = ContentHelpers.RoundToEqualLevels(damage, max, ent.Comp.States + 1);
        _appearance.SetData(ent, RMCDamageVisuals.State, level);
    }
}
