using System.Linq;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Botany;

public sealed class RMCPlantAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly RMCPlantSeedSystem _plantSeed = default!;
    [Dependency] private readonly RMCReagentSystem _reagent = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCPlantAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<RMCPlantAnalyzerComponent, BoundUserInterfaceCheckRangeEvent>(OnUiRangeCheck);

        Subs.BuiEvents<RMCPlantAnalyzerComponent>(RMCPlantAnalyzerUiKey.Key, subs =>
        {
            subs.Event<BoundUIClosedEvent>(OnUiClosed);
        });
    }

    private void OnAfterInteract(Entity<RMCPlantAnalyzerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target || !args.CanReach)
            return;

        if (!HasComp<RMCPlantSeedComponent>(target) &&
            !HasComp<RMCPlantTrayComponent>(target) &&
            !HasComp<RMCProduceComponent>(target))
        {
            return;
        }

        args.Handled = true;
        if (_net.IsClient)
            return;

        ent.Comp.Target = target;
        ent.Comp.User = args.User;
        ent.Comp.NextUpdate = TimeSpan.Zero;
        EnsureComp<RMCPlantAnalyzerActiveComponent>(ent);

        _audio.PlayPvs(ent.Comp.ScanSound, ent);
        _ui.OpenUi(ent.Owner, RMCPlantAnalyzerUiKey.Key, args.User);
        RefreshTarget(ent);
    }

    private void OnUiClosed(Entity<RMCPlantAnalyzerComponent> ent, ref BoundUIClosedEvent args)
    {
        if (_net.IsClient || !args.UiKey.Equals(RMCPlantAnalyzerUiKey.Key))
            return;

        Stop(ent);
    }

    private void OnUiRangeCheck(Entity<RMCPlantAnalyzerComponent> ent, ref BoundUserInterfaceCheckRangeEvent args)
    {
        if (_net.IsClient ||
            args.Result == BoundUserInterfaceRangeResult.Fail ||
            !args.UiKey.Equals(RMCPlantAnalyzerUiKey.Key))
        {
            return;
        }

        if (ent.Comp.User is not { } user ||
            args.Actor.Owner != user ||
            ent.Comp.Target is not { } target ||
            Deleted(target) ||
            !_interaction.InRangeUnobstructed(user, target))
        {
            args.Result = BoundUserInterfaceRangeResult.Fail;
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<RMCPlantAnalyzerActiveComponent, RMCPlantAnalyzerComponent>();
        while (query.MoveNext(out var uid, out _, out var comp))
        {
            if (comp.Target is not { } target)
            {
                RemCompDeferred<RMCPlantAnalyzerActiveComponent>(uid);
                continue;
            }

            var ent = (uid, comp);

            if (!_ui.IsUiOpen(uid, RMCPlantAnalyzerUiKey.Key))
            {
                Stop(ent);
                continue;
            }

            if (Deleted(target))
            {
                Stop(ent);
                continue;
            }

            if (now < comp.NextUpdate || !HasComp<RMCPlantTrayComponent>(target))
                continue;

            comp.NextUpdate = now + comp.UpdateInterval;
            RefreshTarget(ent);
        }
    }

    private void Stop(Entity<RMCPlantAnalyzerComponent> ent)
    {
        if (ent.Comp.Target == null)
            return;

        ent.Comp.Target = null;
        ent.Comp.User = null;
        RemCompDeferred<RMCPlantAnalyzerActiveComponent>(ent);
        Dirty(ent);
        _ui.CloseUi(ent.Owner, RMCPlantAnalyzerUiKey.Key);
    }

    private void RefreshTarget(Entity<RMCPlantAnalyzerComponent> ent)
    {
        if (ent.Comp.Target is not { } target)
            return;

        var comp = ent.Comp;
        comp.PlantName = MetaData(target).EntityName;
        comp.DisplayEntity = target;
        comp.IsTray = false;
        comp.TrayEmpty = false;
        comp.IsProduce = false;
        comp.HasPlantData = false;
        comp.Traits.Clear();
        comp.Chemicals.Clear();

        if (HasComp<RMCPlantSeedComponent>(target))
        {
            var snapshot = _plantSeed.GetOrCreateSeedSnapshot(target);
            ApplyPlantData(comp,
                Get<RMCPlantGrowthComponent>(snapshot),
                Get<RMCPlantHarvestComponent>(snapshot),
                Get<RMCPlantChemicalsComponent>(snapshot),
                Get<RMCPlantTraitsComponent>(snapshot),
                Get<RMCPlantAtmosphericComponent>(snapshot),
                null,
                null,
                snapshot);
        }
        else if (TryComp(target, out RMCPlantTrayComponent? tray))
        {
            comp.IsTray = true;
            comp.WaterLevel = tray.WaterLevel;
            comp.NutritionLevel = tray.NutritionLevel;
            comp.PestLevel = tray.PestLevel;
            comp.WeedLevel = tray.WeedLevel;
            comp.Toxins = tray.Toxins;

            if (tray.PlantSlot.ContainedEntity is not { } plant)
            {
                comp.TrayEmpty = true;
            }
            else
            {
                var plantComp = Comp<RMCPlantComponent>(plant);
                comp.PlantName = Loc.GetString(plantComp.DisplayName);
                comp.DisplayEntity = plant;
                TryComp(plant, out RMCPlantAtmosphericComponent? atmos);
                ApplyPlantData(comp,
                    Comp<RMCPlantGrowthComponent>(plant),
                    Comp<RMCPlantHarvestComponent>(plant),
                    Comp<RMCPlantChemicalsComponent>(plant),
                    Comp<RMCPlantTraitsComponent>(plant),
                    atmos,
                    plantComp,
                    plant);
            }
        }
        else if (TryComp(target, out RMCProduceComponent? produce))
        {
            comp.IsProduce = true;
            comp.PlantName = Loc.GetString(produce.Name);

            var chemicals = Comp<RMCPlantChemicalsComponent>(target);
            comp.Potency = chemicals.Potency;
            AddChemicals(comp, chemicals);
        }

        Dirty(ent);
    }

    private void ApplyPlantData(
        RMCPlantAnalyzerComponent comp,
        RMCPlantGrowthComponent? growth,
        RMCPlantHarvestComponent? harvest,
        RMCPlantChemicalsComponent? chemicals,
        RMCPlantTraitsComponent? traits,
        RMCPlantAtmosphericComponent? atmos,
        RMCPlantComponent? plant,
        EntityUid? live,
        List<IComponent>? snapshot = null)
    {
        comp.HasPlantData = true;

        comp.Endurance = growth?.Endurance ?? 0f;
        comp.Health = plant?.Health ?? comp.Endurance;
        comp.Age = plant?.Age ?? 0;
        comp.Maturation = growth?.Maturation ?? 0f;
        comp.ReadyToHarvest = plant?.Harvest ?? false;
        comp.Lifespan = growth?.Lifespan ?? 0f;
        comp.Production = growth?.Production ?? 0f;
        comp.Viable = growth?.Viable ?? true;

        comp.Yield = harvest?.Yield ?? 0;
        comp.HarvestRepeat = harvest?.HarvestRepeat ?? HarvestType.NoRepeat;
        comp.Seedless = harvest?.Seedless ?? false;

        comp.Potency = chemicals?.Potency ?? 0f;
        AddChemicals(comp, chemicals);

        if (atmos != null)
        {
            comp.MinHeat = atmos.MinHeat;
            comp.MaxHeat = atmos.MaxHeat;
            comp.MinPressure = atmos.MinPressure;
            comp.MaxPressure = atmos.MaxPressure;
        }

        BuildTraitDescriptions(comp, traits, atmos, live, snapshot);
    }

    private void AddChemicals(RMCPlantAnalyzerComponent comp, RMCPlantChemicalsComponent? chemicals)
    {
        if (chemicals == null)
            return;

        foreach (var (reagentId, qty) in chemicals.Chemicals)
        {
            var found = _reagent.TryIndex(reagentId, out var reagent);
            var name = found ? reagent!.LocalizedName : reagentId;
            var color = found ? reagent!.SubstanceColor : Color.White;
            comp.Chemicals.Add(new RMCPlantAnalyzerChemical(reagentId, name, color, RMCPlantSeedSystem.GetChemicalAmount(qty, chemicals.Potency)));
        }
    }

    private void BuildTraitDescriptions(
        RMCPlantAnalyzerComponent comp,
        RMCPlantTraitsComponent? traits,
        RMCPlantAtmosphericComponent? atmos,
        EntityUid? live,
        List<IComponent>? snapshot)
    {
        if (traits == null)
            return;

        if (traits.ToxinsTolerance < comp.SensitiveToleranceThreshold)
            comp.Traits.Add("It is highly sensitive to toxins.");
        else if (traits.ToxinsTolerance > comp.ResistantToleranceThreshold)
            comp.Traits.Add("It is remarkably resistant to toxins.");

        if (traits.PestTolerance < comp.SensitiveToleranceThreshold)
            comp.Traits.Add("It is highly sensitive to pests.");
        else if (traits.PestTolerance > comp.ResistantToleranceThreshold)
            comp.Traits.Add("It is remarkably resistant to pests.");

        if (traits.WeedTolerance < comp.SensitiveToleranceThreshold)
            comp.Traits.Add("It is highly sensitive to weeds.");
        else if (traits.WeedTolerance > comp.ResistantToleranceThreshold)
            comp.Traits.Add("It is remarkably resistant to weeds.");

        switch (GetTrait<RMCPlantTraitCarnivorousComponent>(live, snapshot)?.Level ?? 0)
        {
            case 1:
                comp.Traits.Add("It is carnivorous and will eat tray pests for sustenance.");
                break;
            case 2:
                comp.Traits.Add("It is carnivorous and poses a significant threat to living things around it.");
                break;
        }

        if (GetTrait<RMCPlantTraitParasiteComponent>(live, snapshot) != null)
            comp.Traits.Add("It is capable of parasitizing and gaining sustenance from tray weeds.");

        if (atmos is { AlterTemperature: not 0 })
            comp.Traits.Add($"It will periodically alter the local temperature by {atmos.AlterTemperature} degrees Kelvin.");

        if (GetTrait<RMCPlantTraitBioluminescentComponent>(live, snapshot) is { } bioluminescent)
            comp.Traits.Add($"It is [color={bioluminescent.Color.ToHexNoAlpha()}]bio-luminescent[/color].");

        if (GetTrait<RMCPlantTraitFlowersComponent>(live, snapshot) is { } flowers)
        {
            comp.Traits.Add(flowers.Color is { } flowerColor
                ? $"It has [color={flowerColor.ToHexNoAlpha()}]flowers[/color]."
                : "It has flowers.");
        }
    }

    private static T? Get<T>(List<IComponent> snapshot) where T : class, IComponent
    {
        return snapshot.OfType<T>().FirstOrDefault();
    }

    private T? GetTrait<T>(EntityUid? live, List<IComponent>? snapshot) where T : class, IComponent
    {
        if (snapshot != null)
            return snapshot.OfType<T>().FirstOrDefault();

        return live != null ? CompOrNull<T>(live.Value) : null;
    }
}
