using Content.Shared._RMC14.Botany;
using Content.Shared._RMC14.UserInterface;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.Botany;

[UsedImplicitly]
public sealed class RMCPlantAnalyzerBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey), IRefreshableBui
{
    private static readonly Color StableColor = BotanyUiStyle.StatusStableColor;
    private static readonly Color SeriousColor = BotanyUiStyle.StatusSeriousColor;
    private static readonly Color CriticalColor = BotanyUiStyle.StatusCriticalColor;
    private static readonly Color InfoColor = BotanyUiStyle.StatusInfoColor;
    private static readonly Color EmptyColor = BotanyUiStyle.StatusEmptyColor;

    private static readonly Color GoodColor = BotanyUiStyle.ValueGoodColor;
    private static readonly Color WarnColor = BotanyUiStyle.ValueWarnColor;
    private static readonly Color BadColor = BotanyUiStyle.ValueBadColor;
    private static readonly Color WaterColor = Color.FromHex("#5B9BD5");
    private static readonly Color NeutralColor = Color.White;

    [ViewVariables]
    private RMCPlantAnalyzerWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<RMCPlantAnalyzerWindow>();
        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { } window ||
            !EntMan.TryGetComponent(Owner, out RMCPlantAnalyzerComponent? comp))
        {
            return;
        }

        window.Title = string.IsNullOrEmpty(comp.PlantName) ? "Plant Analyzer" : $"Plant: {comp.PlantName}";

        window.TargetSpriteView.Visible = comp.DisplayEntity.HasValue;
        window.TargetSpriteView.SetEntity(comp.DisplayEntity);

        var (status, detail, statusColor) = GetStatus(comp);
        window.SetStatus(status, detail, statusColor);

        var showTray = comp.IsTray && !comp.TrayEmpty;
        var showLiveBars = comp.HasPlantData && comp.IsTray;

        window.HealthRow.Visible = showLiveBars;
        window.GrowthRow.Visible = showLiveBars;
        window.StatsRow.Visible = comp.HasPlantData || comp.IsProduce;
        window.TrayCard.Visible = showTray;
        window.TraitsCard.Visible = comp.HasPlantData;
        window.ChemicalsCard.Visible = comp.Chemicals.Count > 0;
        window.NotesCard.Visible = comp.Traits.Count > 0;

        if (showLiveBars)
        {
            var healthRatio = comp.Endurance > 0f ? comp.Health / comp.Endurance : 1f;
            window.SetHealth(healthRatio, $"{comp.Health:0.#} / {comp.Endurance:0.#}", RatioColor(healthRatio));

            var growthRatio = comp.ReadyToHarvest ? 1f : comp.Maturation > 0f ? comp.Age / comp.Maturation : 1f;
            window.SetGrowth(growthRatio,
                comp.ReadyToHarvest ? "Ready to harvest" : $"{comp.Age} / {comp.Maturation:0.#} cycles",
                GoodColor);
        }

        if (comp.HasPlantData)
        {
            window.SetStatChips(new (string, string, Color)[]
            {
                ("Yield", $"{comp.Yield}", GoodColor),
                ("Potency", $"{comp.Potency:0.#}", WarnColor),
                ("Endurance", $"{comp.Endurance:0.#}", BadColor),
            });

            window.SetTraitRows(new (string, string, Color)[]
            {
                ("Lifespan", $"{comp.Lifespan:0.#}", NeutralColor),
                ("Maturation", $"{comp.Maturation:0.#}", NeutralColor),
                ("Production", $"{comp.Production:0.#}", NeutralColor),
                ("Harvest", BotanyUiStyle.GetHarvestLabel(comp.HarvestRepeat), NeutralColor),
                ("Seedless", comp.Seedless ? "Yes" : "No", NeutralColor),
                ("Viable", comp.Viable ? "Yes" : "No", comp.Viable ? GoodColor : BadColor),
                ("Temperature", $"{comp.MinHeat:0.#}–{comp.MaxHeat:0.#} K", NeutralColor),
                ("Pressure", $"{comp.MinPressure:0.#}–{comp.MaxPressure:0.#} kPa", NeutralColor),
            });
        }
        else if (comp.IsProduce)
        {
            window.SetStatChips(new (string, string, Color)[] { ("Potency", $"{comp.Potency:0.#}", WarnColor) });
        }

        if (showTray)
        {
            var max = comp.TrayLevelMax > 0f ? comp.TrayLevelMax : 1f;
            var hazardMax = comp.TrayHazardMax > 0f ? comp.TrayHazardMax : 1f;
            window.SetTrayRows(new (string, string, Color)[]
            {
                ("Water", $"{comp.WaterLevel:0.#} / {max:0}", SupplyColor(comp.WaterLevel / max, WaterColor)),
                ("Nutrients", $"{comp.NutritionLevel:0.#} / {max:0}", SupplyColor(comp.NutritionLevel / max, GoodColor)),
                ("Pests", $"{comp.PestLevel:0.#} / {hazardMax:0}", HazardColor(comp.PestLevel / hazardMax)),
                ("Weeds", $"{comp.WeedLevel:0.#} / {hazardMax:0}", HazardColor(comp.WeedLevel / hazardMax)),
                ("Toxins", $"{comp.Toxins:0.#} / {hazardMax:0}", HazardColor(comp.Toxins / hazardMax)),
            });
        }

        window.SetChemicals(comp.Chemicals);
        window.SetNotes(comp.Traits);
    }

    private static (string Status, string Detail, Color Color) GetStatus(RMCPlantAnalyzerComponent comp)
    {
        if (comp.TrayEmpty)
            return ("EMPTY", "There is no plant in this tray.", EmptyColor);

        if (comp.IsProduce)
            return ("PRODUCE", comp.PlantName, InfoColor);

        if (!comp.IsTray)
        {
            return comp.Viable
                ? ("SEEDS", comp.PlantName, InfoColor)
                : ("NON-VIABLE", comp.PlantName, CriticalColor);
        }

        var healthRatio = comp.Endurance > 0f ? comp.Health / comp.Endurance : 1f;
        if (comp.Health <= 0f)
            return ("DEAD", comp.PlantName, CriticalColor);

        if (comp.ReadyToHarvest)
            return ("HARVEST", "Ready to harvest", StableColor);

        if (healthRatio < 0.35f)
            return ("CRITICAL", comp.PlantName, CriticalColor);

        if (healthRatio < 0.7f)
            return ("DAMAGED", comp.PlantName, SeriousColor);

        return ("HEALTHY", comp.PlantName, StableColor);
    }

    private static Color RatioColor(float ratio)
    {
        return ratio < 0.35f ? BadColor : ratio < 0.7f ? WarnColor : GoodColor;
    }

    private static Color SupplyColor(float ratio, Color good)
    {
        return ratio < 0.2f ? BadColor : ratio < 0.5f ? WarnColor : good;
    }

    private static Color HazardColor(float ratio)
    {
        return ratio >= 0.7f ? BadColor : ratio >= 0.4f ? WarnColor : NeutralColor;
    }
}
