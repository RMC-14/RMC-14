using System.Text.Json.Serialization;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._CM14.Chemistry.Effects;

public sealed partial class RemoveDamage : ReagentEffect
{
    [DataField(required: true)]
    [JsonPropertyName("type")]
    public ProtoId<DamageTypePrototype> Type;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if (!prototype.TryIndex(Type, out var type))
            return null;

        return $"Removes all {type.LocalizedName} damage";
    }

    public override void Effect(ReagentEffectArgs args)
    {
        if (args.Scale < 0.95f)
            return;

        if (!args.EntityManager.TryGetComponent(args.SolutionEntity, out DamageableComponent? damageable) ||
            !damageable.Damage.DamageDict.TryGetValue(Type, out var value))
        {
            return;
        }

        var damage = new DamageSpecifier
        {
            DamageDict =
            {
                [Type] = -value
            }
        };

        args.EntityManager.System<DamageableSystem>()
            .TryChangeDamage(args.SolutionEntity, damage, true, interruptsDoAfters: false);
    }
}
