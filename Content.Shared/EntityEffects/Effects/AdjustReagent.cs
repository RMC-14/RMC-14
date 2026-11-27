using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared.Body.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects
{
    public sealed partial class AdjustReagent : EntityEffect
    {
        /// <summary>
        ///     The reagent ID to remove. Only one of this and <see cref="Group"/> should be active.
        /// </summary>
        [DataField]
        public ProtoId<ReagentPrototype>? Reagent = null;
        // TODO use ReagentId

        /// <summary>
        ///     The metabolism group to remove, if the reagent satisfies any.
        ///     Only one of this and <see cref="Reagent"/> should be active.
        /// </summary>
        [DataField]
        public ProtoId<MetabolismGroupPrototype>? Group = null;

        [DataField(required: true)]
        public FixedPoint2 Amount = default!;

        public override void Effect(EntityEffectBaseArgs args)
        {
            if (args is EntityEffectReagentArgs reagentArgs)
            {
                if (reagentArgs.Source == null)
                    return;

                var amount = Amount;
                amount *= reagentArgs.Scale;

                if (Reagent is { } reagent)
                {
                    if (amount < 0 && reagentArgs.Source.ContainsPrototype(reagent))
                        reagentArgs.Source.RemoveReagent(reagent, -amount);
                    if (amount > 0)
                        reagentArgs.Source.AddReagent(reagent, amount);
                }
                else if (Group is { } group)
                {
                    var prototypeMan = IoCManager.Resolve<IPrototypeManager>();
                    foreach (var quant in reagentArgs.Source.Contents.ToArray())
                    {
                        var proto = prototypeMan.IndexReagent<ReagentPrototype>(quant.Reagent.Prototype);
                        if (proto.Metabolisms != null && proto.Metabolisms.ContainsKey(group))
                        {
                            if (amount < 0)
                                reagentArgs.Source.RemoveReagent(quant.Reagent, amount);
                            if (amount > 0)
                                reagentArgs.Source.AddReagent(quant.Reagent, amount);
                        }
                    }
                }
                return;
            }

            // TODO: Someone needs to figure out how to do this for non-reagent effects.
            throw new NotImplementedException();
        }

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        {
            var reagentSystem = entSys.GetEntitySystem<RMCReagentSystem>();
            if (Reagent is { } reagent && reagentSystem.TryIndex(reagent, out var reagentProto))
            {
                return Loc.GetString("reagent-effect-guidebook-adjust-reagent-reagent",
                    ("chance", Probability),
                    ("deltasign", MathF.Sign(Amount.Float())),
                    ("reagent", reagentProto.LocalizedName),
                    ("amount", MathF.Abs(Amount.Float())));
            }
            else if (Group is { } group && prototype.TryIndex(group, out MetabolismGroupPrototype? groupProto))
            {
                return Loc.GetString("reagent-effect-guidebook-adjust-reagent-group",
                    ("chance", Probability),
                    ("deltasign", MathF.Sign(Amount.Float())),
                    ("group", groupProto.LocalizedName),
                    ("amount", MathF.Abs(Amount.Float())));
            }

            throw new NotImplementedException();
        }
    }
}

