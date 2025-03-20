using Content.Server._RMC14.Atmos;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.Explosion;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System.Text.Json.Serialization;

namespace Content.Server._RMC14.EntityEffects.Effects;
[DataDefinition]

public sealed partial class RMCTileFireReactionEffect : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    => Loc.GetString("reagent-effect-guidebook-tile-fire-reaction-effect", ("chance", Probability));
    public override LogImpact LogImpact => LogImpact.High;

}
