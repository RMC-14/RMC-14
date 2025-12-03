using Content.Shared._RMC14.Temperature;
using Content.Shared.EntityEffects;
using Content.Shared.Temperature.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Cryometabolizing : RMCChemicalEffect
{
    public override string Abbreviation => "CMB";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Boosts other chemical effects by [color=green]{Level * 0.5f:F1}[/color].\n" +
               $"Does not metabolize above -63.15ºC (-81.67ºF).";
    }

    protected override void ReagentBoost(EntityEffectReagentArgs args, ref float boost)
    {
        boost = Level * 0.5f;
    }

    public override bool CanMetabolize(EntityUid target)
    {
        var temperature = IoCManager.Resolve<IEntityManager>().System<SharedRMCTemperatureSystem>();
        return temperature.GetTemperature(target) < 210;
    }
}
