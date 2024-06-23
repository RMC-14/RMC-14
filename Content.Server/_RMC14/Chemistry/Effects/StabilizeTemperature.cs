using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Chemistry.Effects;

public sealed partial class StabilizeTemperature : ReagentEffect
{
    [DataField(required: true)]
    public float Stable;

    [DataField(required: true)]
    public float Change;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Stabilizes the temperature of the body that it is in to {Stable} degrees, by {Change} degrees at a time";
    }

    public override void Effect(ReagentEffectArgs args)
    {
        if (!args.EntityManager.TryGetComponent(args.SolutionEntity, out TemperatureComponent? comp))
            return;

        var current = comp.CurrentTemperature;
        if (Math.Abs(current - Stable) < 0.01)
            return;

        var sys = args.EntityManager.EntitySysManager.GetEntitySystem<TemperatureSystem>();
        var temp = current > Stable
            ? Math.Max(Stable, current - Change * args.Scale)
            : Math.Min(Stable, current + Change * args.Scale);

        sys.ForceChangeTemperature(args.SolutionEntity, temp, comp);
    }
}
