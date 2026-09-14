using Content.Shared._RMC14.Botany;
using Content.Shared.Atmos;

namespace Content.Server._RMC14.Botany;

/// <summary>
/// Gas consumption from, and production into, the tray's containing atmosphere.
/// </summary>
public sealed class RMCConsumeExudeGasSystem : EntitySystem
{
    [Dependency] private readonly RMCPlantTraySystem _plantTray = default!;

    public const float HydroponicsSpeedMultiplier = 1f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCPlantGrowEvent>(OnPlantGrow);
    }

    private void OnPlantGrow(ref RMCPlantGrowEvent args)
    {
        if (!TryComp(args.Plant, out RMCPlantComponent? plantComp))
            return;

        var tray = (args.Tray, Comp<RMCPlantTrayComponent>(args.Tray));
        var ent = (args.Plant, plantComp);
        TickConsume(ent, tray, args.Environment);
        TickExude(args.Plant, args.Environment);
    }

    public void TickConsume(Entity<RMCPlantComponent> plant, Entity<RMCPlantTrayComponent> tray, GasMixture environment)
    {
        var missingGas = 0;

        if (!TryComp(plant.Owner, out RMCConsumeExudeGasComponent? gas) || gas.ConsumeGasses.Count == 0)
        {
            _plantTray.SetMissingGas((tray.Owner, tray.Comp), missingGas);
            return;
        }

        foreach (var (gasType, amount) in gas.ConsumeGasses)
        {
            if (environment.GetMoles(gasType) < amount)
            {
                missingGas++;
                continue;
            }

            environment.AdjustMoles(gasType, -amount);
        }

        _plantTray.SetMissingGas((tray.Owner, tray.Comp), missingGas);

        if (missingGas > 0)
        {
            // no atmos
            // var growth = Comp<RMCPlantGrowthComponent>(plant.Owner);
            // _plantTray.AdjustHealth((plant.Owner, plant.Comp, growth), -missingGas * HydroponicsSpeedMultiplier);
            if (tray.Comp.DrawWarnings)
                tray.Comp.UpdateSpriteAfterUpdate = true;
        }
    }

    public void TickExude(EntityUid plant, GasMixture environment)
    {
        if (!TryComp(plant, out RMCConsumeExudeGasComponent? gas) || gas.ExudeGasses.Count == 0)
            return;

        var potency = TryComp(plant, out RMCPlantChemicalsComponent? chemicals) ? chemicals.Potency : 0f;
        var exudeCount = gas.ExudeGasses.Count;

        foreach (var (gasType, amount) in gas.ExudeGasses)
        {
            environment.AdjustMoles(gasType,
                MathF.Max(1f, MathF.Round(amount * MathF.Round(potency) / exudeCount)));
        }
    }
}
