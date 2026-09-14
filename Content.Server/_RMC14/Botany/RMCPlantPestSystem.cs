using Content.Shared._RMC14.Botany;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Botany;

/// <summary>
/// Pest population growth, carnivorous consumption, & pest tolerance damage
/// </summary>
public sealed class RMCPlantPestSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
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
        TickSpawn(tray);
        Tick(ent, tray, Comp<RMCPlantTraitsComponent>(args.Plant));
    }

    /// <summary>
    /// Small chance for the pest population to increase. Only runs for a live, non-dead plant.
    /// </summary>
    public void TickSpawn(Entity<RMCPlantTrayComponent> tray)
    {
        if (!_random.Prob(0.03f))
            return;

        _plantTray.AdjustPestLevel((tray.Owner, tray.Comp), 0.1f * HydroponicsSpeedMultiplier);
        if (tray.Comp.DrawWarnings)
            tray.Comp.UpdateSpriteAfterUpdate = true;
    }

    public void Tick(Entity<RMCPlantComponent> plant, Entity<RMCPlantTrayComponent> tray, RMCPlantTraitsComponent traits)
    {
        if (tray.Comp.PestLevel <= 0)
            return;

        var growth = Comp<RMCPlantGrowthComponent>(plant.Owner);

        if (HasComp<RMCPlantTraitCarnivorousComponent>(plant.Owner))
        {
            _plantTray.AdjustPestLevel((tray.Owner, tray.Comp), -HydroponicsSpeedMultiplier);
            _plantTray.AdjustHealth((plant.Owner, plant.Comp, growth), HydroponicsSpeedMultiplier);
        }
        else if (tray.Comp.PestLevel >= traits.PestTolerance)
        {
            _plantTray.AdjustHealth((plant.Owner, plant.Comp, growth), -HydroponicsSpeedMultiplier);
        }

        if (tray.Comp.DrawWarnings)
            tray.Comp.UpdateSpriteAfterUpdate = true;
    }
}
