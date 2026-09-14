using Content.Shared._RMC14.Botany;

namespace Content.Server._RMC14.Botany;

public sealed class RMCPlantToxinSystem : EntitySystem
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

        Tick((args.Plant, plantComp), (args.Tray, Comp<RMCPlantTrayComponent>(args.Tray)), Comp<RMCPlantTraitsComponent>(args.Plant));
    }

    public void Tick(Entity<RMCPlantComponent> plant, Entity<RMCPlantTrayComponent> tray, RMCPlantTraitsComponent traits)
    {
        if (tray.Comp.Toxins <= 0)
            return;

        var toxinUptake = MathF.Max(1, MathF.Floor(tray.Comp.Toxins / 10f));
        if (tray.Comp.Toxins > traits.ToxinsTolerance)
        {
            var growth = Comp<RMCPlantGrowthComponent>(plant.Owner);
            _plantTray.AdjustHealth((plant.Owner, plant.Comp, growth), -toxinUptake);
        }

        _plantTray.AdjustToxins((tray.Owner, tray.Comp), -toxinUptake);
        if (tray.Comp.DrawWarnings)
            tray.Comp.UpdateSpriteAfterUpdate = true;
    }
}
