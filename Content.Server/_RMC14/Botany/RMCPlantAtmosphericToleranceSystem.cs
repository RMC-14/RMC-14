using Content.Shared._RMC14.Botany;
using Content.Shared.Atmos;

namespace Content.Server._RMC14.Botany;

/// <summary>
/// Pressure/heat tolerance damage, using min/max tolerance windows.
/// </summary>
public sealed class RMCPlantAtmosphericToleranceSystem : EntitySystem
{
    [Dependency] private readonly RMCPlantTraySystem _plantTray = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCPlantGrowEvent>(OnPlantGrow);
    }

    private void OnPlantGrow(ref RMCPlantGrowEvent args)
    {
        if (!TryComp(args.Plant, out RMCPlantComponent? plantComp))
            return;

        if (!TryComp(args.Plant, out RMCPlantAtmosphericComponent? atmos))
            return;

        Tick((args.Plant, plantComp), (args.Tray, Comp<RMCPlantTrayComponent>(args.Tray)), atmos, args.Environment, args.HealthMod);
    }

    public void Tick(Entity<RMCPlantComponent> plant, Entity<RMCPlantTrayComponent> tray, RMCPlantAtmosphericComponent atmos, GasMixture environment, float healthMod)
    {
        var growth = Comp<RMCPlantGrowthComponent>(plant.Owner);

        var pressure = environment.Pressure;
        if (pressure < atmos.MinPressure || pressure > atmos.MaxPressure)
        {
            // no atmos
            // _plantTray.AdjustHealth((plant.Owner, plant.Comp, growth), -healthMod);
            // _plantTray.SetImproperPressure((tray.Owner, tray.Comp), true);
            // if (tray.Comp.DrawWarnings)
            //     tray.Comp.UpdateSpriteAfterUpdate = true;
        }
        else
        {
            _plantTray.SetImproperPressure((tray.Owner, tray.Comp), false);
        }

        var temperature = environment.Temperature;
        if (temperature < atmos.MinHeat || temperature > atmos.MaxHeat)
        {
            // no atmos
            // _plantTray.AdjustHealth((plant.Owner, plant.Comp, growth), -healthMod);
            // _plantTray.SetImproperHeat((tray.Owner, tray.Comp), true);
            // if (tray.Comp.DrawWarnings)
            //     tray.Comp.UpdateSpriteAfterUpdate = true;
        }
        else
        {
            _plantTray.SetImproperHeat((tray.Owner, tray.Comp), false);
        }
    }
}
