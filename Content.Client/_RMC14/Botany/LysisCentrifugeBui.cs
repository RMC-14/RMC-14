using Content.Shared._RMC14.Botany;
using Content.Shared._RMC14.UserInterface;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Botany;

[UsedImplicitly]
public sealed class LysisCentrifugeBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey), IRefreshableBui
{
    [ViewVariables]
    private LysisCentrifugeWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<LysisCentrifugeWindow>();
        _window.Title = "Lysis Isolation Centrifuge";

        _window.ProcessSeedButton.OnPressed += _ => SendMessage(new LysisCentrifugeProcessSeedBuiMsg());
        _window.EjectSeedButton.OnPressed += _ => SendMessage(new LysisCentrifugeEjectSeedBuiMsg());
        _window.ClearBufferButton.OnPressed += _ => SendMessage(new LysisCentrifugeClearBufferBuiMsg());
        _window.EjectDiscButton.OnPressed += _ => SendMessage(new LysisCentrifugeEjectDiscBuiMsg());

        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { } window ||
            !EntMan.TryGetComponent(Owner, out LysisCentrifugeComponent? comp))
        {
            return;
        }

        window.SeedSpriteView.Visible = comp.HasSeed && comp.SeedEntityNet.HasValue;
        if (comp.SeedEntityNet is { } seedNet)
            window.SeedSpriteView.SetEntity(seedNet);
        else
            window.SeedSpriteView.SetEntity((EntityUid?) null);

        if (comp.HasSeed)
        {
            var seedText = comp.SeedPacketName is { } packetName
                ? BotanyUiStyle.ShortSeedName(packetName)
                : "[Seed inserted]";
            window.SetSeedStatus(seedText, comp.SeedPacketName);

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
            window.SetSeedTraitRows([]);
        }

        if (comp.GenomeName is { } genomeName)
        {
            var ratio = comp.MaxDegradation > 0
                ? comp.Degradation / (float) comp.MaxDegradation
                : 0f;
            var color = ratio >= 0.75f ? BotanyUiStyle.RedColor : ratio >= 0.4f ? BotanyUiStyle.TanColor : BotanyUiStyle.GreenColor;

            window.SetGenomeStatus(genomeName, color);
            window.DegradationMetric.SetAccentColor(color);
            window.DegradationMetric.Update("Degradation", $"{comp.Degradation}/{comp.MaxDegradation}", progress: ratio);
        }
        else
        {
            window.ClearGenomeStatus();
            window.DegradationMetric.SetAccentColor(BotanyUiStyle.GreenColor);
            window.DegradationMetric.Update("Degradation", "—", progress: 0f);
        }

        if (comp.HasDisc)
            window.SetDiscStatus("[Disc inserted]");
        else
            window.ClearDiscStatus();

        window.GenesSection.Visible = comp.GenomeName != null;
        window.GeneRowsContainer.DisposeAllChildren();

        if (comp.GenomeName == null)
            return;

        foreach (var slot in comp.GeneSlots)
        {
            var typeName = slot.GeneType.GetLabel();
            var text = slot.AlreadyOnDisc
                ? $"{slot.ObfuscatedCode} - {typeName}: ✓"
                : $"{slot.ObfuscatedCode} - {typeName}:";

            string? extractTooltip = null;
            if (slot.AlreadyOnDisc)
                extractTooltip = "This gene sequence is already on the disc.";
            else if (comp.DiscFull)
                extractTooltip = "Disc already contains a gene sequence.";

            var row = new LysisCentrifugeGeneRow();
            row.SetGene(text, slot.GeneType.GetDescription(), extractTooltip);

            var geneType = slot.GeneType;
            row.ExtractButton.OnPressed += _ => SendMessage(new LysisCentrifugeExtractGeneBuiMsg(geneType));

            window.GeneRowsContainer.AddChild(row);
        }
    }
}
