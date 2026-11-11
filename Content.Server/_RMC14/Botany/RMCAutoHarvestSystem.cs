using Content.Shared._RMC14.Botany;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;

namespace Content.Server._RMC14.Botany;

public sealed class RMCAutoHarvestSystem : EntitySystem
{
    [Dependency] private readonly RMCPlantHarvestSystem _plantHarvest = default!;
    [Dependency] private readonly RMCPlantTraySystem _plantTray = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCPlantTrayComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs);
        SubscribeLocalEvent<RMCAutoHarvestUpgradeComponent, AfterInteractEvent>(OnUpgradeInteract);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RMCAutoHarvestComponent, RMCPlantTrayComponent>();
        while (query.MoveNext(out var uid, out _, out var tray))
        {
            if (tray.PlantSlot.ContainedEntity is not { } plant)
                continue;

            var plantComp = Comp<RMCPlantComponent>(plant);
            if (!plantComp.Harvest || plantComp.Dead)
                continue;

            _plantHarvest.AutoHarvest(uid, tray);
        }
    }

    private void OnGetVerbs(Entity<RMCPlantTrayComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;

        args.Verbs.Add(new ActivationVerb
        {
            Text = "Flush tray",
            Priority = -1,
            Act = () => FlushTray(ent, user),
        });
    }

    private void FlushTray(Entity<RMCPlantTrayComponent> ent, EntityUid user)
    {
        var comp = ent.Comp;

        if (comp.WaterLevel <= 0 && comp.NutritionLevel <= 0 && comp.Toxins <= 0)
        {
            _popup.PopupCursor("The tray is already empty.", user);
            return;
        }

        _plantTray.SetWaterLevel(ent.AsNullable(), 0);
        _plantTray.SetNutritionLevel(ent.AsNullable(), 0);
        _plantTray.SetToxins(ent.AsNullable(), 0);

        if (_solution.ResolveSolution(ent.Owner, comp.SoilSolutionName, ref comp.SoilSolution, out _))
            _solution.RemoveAllSolution(comp.SoilSolution.Value);

        _plantTray.UpdateSprite(ent.Owner, comp);
        _popup.PopupCursor("You flush the tray, draining the soil solution.", user);
    }

    private void OnUpgradeInteract(Entity<RMCAutoHarvestUpgradeComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null)
            return;

        if (!HasComp<RMCPlantTrayComponent>(args.Target))
            return;

        args.Handled = true;

        if (HasComp<RMCAutoHarvestComponent>(args.Target))
        {
            _popup.PopupCursor("An auto-harvester is already installed.", args.User);
            return;
        }

        AddComp<RMCAutoHarvestComponent>(args.Target.Value);
        QueueDel(ent);
        _popup.PopupCursor("Auto-harvester installed.", args.User);
    }
}
