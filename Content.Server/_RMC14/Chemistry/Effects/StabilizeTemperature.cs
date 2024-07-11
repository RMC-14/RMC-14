using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Chemistry.Effects;

public sealed partial class StabilizeTemperature : EntityEffect
{
    [DataField(required: true)]
    public float Stable;

    [DataField(required: true)]
    public float Change;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Stabilizes the temperature of the body that it is in to {Stable} degrees, by {Change} degrees at a time";
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!args.EntityManager.TryGetComponent(args.TargetEntity, out TemperatureComponent? comp))
            return;

        var current = comp.CurrentTemperature;
        if (Math.Abs(current - Stable) < 0.01)
            return;

        var change = Change;
        if (args is EntityEffectReagentArgs reagentArgs)
            change *= reagentArgs.Scale.Float();

        var sys = args.EntityManager.EntitySysManager.GetEntitySystem<TemperatureSystem>();
        var temp = current > Stable
            ? Math.Max(Stable, current - change)
            : Math.Min(Stable, current + change);

        sys.ForceChangeTemperature(args.TargetEntity, temp, comp);
    }
}
