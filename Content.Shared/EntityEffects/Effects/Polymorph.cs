using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class Polymorph : EventEntityEffect<Polymorph>
{
    /// <summary>
    ///     What polymorph prototype is used on effect
    /// </summary>
    [DataField("prototype")]
    public ProtoId<PolymorphPrototype> PolymorphPrototype { get; set; }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    => Loc.GetString("reagent-effect-guidebook-make-polymorph",
            ("chance", Probability), ("entityname",
                prototype.Index<EntityPrototype>(prototype.Index<PolymorphPrototype>(PolymorphPrototype).Configuration.Entity).Name));
}
