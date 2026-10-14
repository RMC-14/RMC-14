using Content.Shared._RMC14.Botany;
using Content.Shared._RMC14.UserInterface;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Botany;

[UsedImplicitly]
public sealed class RMCPlantAnalyzerBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey), IRefreshableBui
{
    [ViewVariables]
    private RMCPlantAnalyzerWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<RMCPlantAnalyzerWindow>();
        _window.Title = "Plant Analyzer";

        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { } window ||
            !EntMan.TryGetComponent(Owner, out RMCPlantAnalyzerComponent? comp))
        {
            return;
        }

        window.TargetSpriteView.Visible = comp.DisplayEntity.HasValue;
        window.TargetSpriteView.SetEntity(comp.DisplayEntity);
        window.TargetNameLabel.Text = comp.PlantName;

        if (comp.TrayEmpty)
            window.SetStatus("[Tray is empty]", BotanyUiStyle.RedColor);
        else
            window.SetStatus("[Analyzed]", BotanyUiStyle.GreenColor);

        var showTray = comp.IsTray && !comp.TrayEmpty;
        window.VitalsSection.Visible = comp.HasPlantData;
        window.TraitsSection.Visible = comp.HasPlantData || comp.IsProduce;
        window.ConditionsSection.Visible = comp.HasPlantData;
        window.TraySection.Visible = showTray;
        window.NotesSection.Visible = comp.Traits.Count > 0;
        window.ChemicalsSection.Visible = comp.Chemicals.Count > 0;
        window.MetricsColumn.Visible = comp.HasPlantData || showTray;

        if (comp.HasPlantData)
        {
            var healthRatio = comp.Endurance > 0f ? comp.Health / comp.Endurance : 1f;
            window.HealthMetric.Update("Health", $"{comp.Health:0.#} / {comp.Endurance:0.#}", progress: healthRatio);

            var growthRatio = comp.Maturation > 0f ? comp.Age / comp.Maturation : 1f;
            window.GrowthMetric.Update("Growth",
                comp.ReadyToHarvest ? "Ready to harvest" : $"{comp.Age} / {comp.Maturation:0.#} cycles",
                progress: comp.ReadyToHarvest ? 1f : growthRatio);

            window.TemperatureMetric.Update("Temperature", $"{comp.MinHeat:0.#}–{comp.MaxHeat:0.#} K");
            window.PressureMetric.Update("Pressure", $"{comp.MinPressure:0.#}–{comp.MaxPressure:0.#} kPa");

            window.SetTraitRows(new (string, string)[]
            {
                ("Yield", $"{comp.Yield}"),
                ("Potency", $"{comp.Potency:0.#}"),
                ("Lifespan", $"{comp.Lifespan:0.#}"),
                ("Production", $"{comp.Production:0.#}"),
                ("Harvest", BotanyUiStyle.GetHarvestLabel(comp.HarvestRepeat)),
                ("Seedless", comp.Seedless ? "Yes" : "No"),
                ("Viable", comp.Viable ? "Yes" : "No"),
            });
        }
        else if (comp.IsProduce)
        {
            window.SetTraitRows(new (string, string)[] { ("Potency", $"{comp.Potency:0.#}") });
        }

        if (showTray)
        {
            var max = comp.TrayLevelMax > 0f ? comp.TrayLevelMax : 1f;
            window.WaterMetric.Update("Water", $"{comp.WaterLevel:0.#}", progress: comp.WaterLevel / max);
            window.NutritionMetric.Update("Nutrition", $"{comp.NutritionLevel:0.#}", progress: comp.NutritionLevel / max);
            window.PestMetric.Update("Pest", $"{comp.PestLevel:0.#}", progress: comp.PestLevel / max);
            window.WeedMetric.Update("Weed", $"{comp.WeedLevel:0.#}", progress: comp.WeedLevel / max);
            window.ToxinMetric.Update("Toxin", $"{comp.Toxins:0.#}", progress: comp.Toxins / max);
        }

        window.SetNotes(comp.Traits);
        window.SetChemicals(comp.Chemicals);
    }
}
