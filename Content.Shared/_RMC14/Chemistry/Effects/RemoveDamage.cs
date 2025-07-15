using System.Text.Json.Serialization;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects;

public sealed partial class RemoveDamage : EntityEffect
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

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is EntityEffectReagentArgs reagent && reagent.Scale < 0.95f)
            return;

        if (!args.EntityManager.TryGetComponent(args.TargetEntity, out DamageableComponent? damageable))
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
            .TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
