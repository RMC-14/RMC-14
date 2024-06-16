using System.Text.Json.Serialization;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._CM14.Chemistry.Effects;

public sealed partial class RemoveDamage : ReagentEffect
{
    [DataField(required: true)]
    [JsonPropertyName("group")]
    public ProtoId<DamageGroupPrototype> Group;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if (!prototype.TryIndex(Group, out var type))
            return null;

        return $"Removes all {type.LocalizedName} damage";
    }

    public override void Effect(ReagentEffectArgs args)
    {
        if (args.Scale < 0.95f)
            return;

        if (!args.EntityManager.TryGetComponent(args.SolutionEntity, out DamageableComponent? damageable))
            return;

        var prototypes = IoCManager.Resolve<IPrototypeManager>();
        if (!prototypes.TryIndex(Group, out var group))
            return;

        var damage = new DamageSpecifier();
        foreach (var type in group.DamageTypes)
        {
            if (damageable.Damage.DamageDict.TryGetValue(type, out var amount))
                damage.DamageDict[type] = -amount;
        }

        args.EntityManager.System<DamageableSystem>()
            .TryChangeDamage(args.SolutionEntity, damage, true, interruptsDoAfters: false);
    }
}
