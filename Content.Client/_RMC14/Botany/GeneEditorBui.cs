using Content.Shared._RMC14.Botany;
using Content.Shared._RMC14.UserInterface;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Botany;

[UsedImplicitly]
public sealed class GeneEditorBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey), IRefreshableBui
{
    [ViewVariables]
    private GeneEditorWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GeneEditorWindow>();
        _window.Title = "Bioballistic Delivery System";

        _window.ApplyButton.OnPressed += _ => SendMessage(new GeneEditorApplyBuiMsg());
        _window.EjectButton.OnPressed += _ => SendMessage(new GeneEditorEjectBuiMsg());

        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { } window ||
            !EntMan.TryGetComponent(Owner, out GeneEditorComponent? comp))
        {
            return;
        }

        if (comp.HasDisc)
            window.SetDiscStatus("[Disc loaded]");
        else
            window.ClearDiscStatus();

        if (comp.HasDisc && comp.DiscSource is { } discSource)
        {
            window.DiscSourceLabel.Text = $"Source: {discSource}";
            window.DiscSourceLabel.Visible = true;
        }
        else
        {
            window.DiscSourceLabel.Visible = false;
        }

        window.NoDiscGenesLabel.Visible = comp.HasDisc && comp.DiscGeneLabels.Count == 0;
        window.SetDiscGenes(comp.HasDisc ? comp.DiscGeneLabels : []);

        window.SeedSpriteView.Visible = comp.HasSeed && comp.SeedEntityNet.HasValue;
        if (comp.SeedEntityNet is { } seedNet)
            window.SeedSpriteView.SetEntity(seedNet);
        else
            window.SeedSpriteView.SetEntity((EntityUid?) null);

        if (comp.HasSeed)
        {
            var seedText = comp.SeedName is { } seedName
                ? BotanyUiStyle.ShortSeedName(seedName)
                : "[Unknown seed]";
            window.SetSeedStatus(seedText, comp.SeedName);

            var ratio = comp.MaxEditCount > 0
                ? comp.SeedEditCount / (float) comp.MaxEditCount
                : 0f;
            var color = ratio >= 1f ? BotanyUiStyle.RedColor : BotanyUiStyle.GreenColor;
            window.EditCountMetric.SetAccentColor(color);
            window.EditCountMetric.Update("Gene Edits", $"{comp.SeedEditCount}/{comp.MaxEditCount}", progress: ratio);

            window.SetSeedTraitRows(new (string, string)[]
            {
                ("Endurance", $"{comp.SeedEndurance:0.#}"),
                ("Yield", $"{comp.SeedYield}"),
                ("Potency", $"{comp.SeedPotency:0.#}"),
                ("Lifespan", $"{comp.SeedLifespan:0.#}"),
                ("Maturation", $"{comp.SeedMaturation:0.#}"),
                ("Production", $"{comp.SeedProduction:0.#}"),
                ("Harvest", BotanyUiStyle.GetHarvestLabel(comp.SeedHarvestRepeat)),
                ("Seedless", comp.SeedSeedless ? "Yes" : "No"),
                ("Viable", comp.SeedViable ? "Yes" : "No"),
            });
        }
        else
        {
            window.ClearSeedStatus();
            window.EditCountMetric.SetAccentColor(BotanyUiStyle.GreenColor);
            window.EditCountMetric.Update("Gene Edits", "—", progress: 0f);
            window.SetSeedTraitRows([]);
        }
    }
}
