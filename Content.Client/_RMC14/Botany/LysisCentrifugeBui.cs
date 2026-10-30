using Content.Client._RMC14.UserInterface;
using Content.Shared._RMC14.Botany;
using Content.Shared._RMC14.UserInterface;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Maths;

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
        _window.Title = EntMan.GetComponentOrNull<MetaDataComponent>(Owner)?.EntityName ?? string.Empty;

        _window.ProcessSeedButton.OnPressed += _ => SendPredictedMessage(new LysisCentrifugeProcessSeedBuiMsg());
        _window.EjectSeedButton.OnPressed += _ => SendPredictedMessage(new LysisCentrifugeEjectSeedBuiMsg());
        _window.ClearBufferButton.OnPressed += _ => SendPredictedMessage(new LysisCentrifugeClearBufferBuiMsg());
        _window.EjectDiscButton.OnPressed += _ => SendPredictedMessage(new LysisCentrifugeEjectDiscBuiMsg());

        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { } window ||
            !EntMan.TryGetComponent(Owner, out LysisCentrifugeComponent? comp))
        {
            return;
        }

        var itemSlots = EntMan.System<ItemSlotsSystem>();
        var seed = itemSlots.GetItemOrNull(Owner, comp.SeedSlotId);
        var disc = itemSlots.GetItemOrNull(Owner, comp.DiscSlotId);
        var hasGenome = comp.GenomeName != null;

        window.SeedSpriteView.Visible = seed != null;
        window.SeedSpriteView.SetEntity(seed);

        var seedName = seed != null ? EntMan.GetComponentOrNull<MetaDataComponent>(seed.Value)?.EntityName : null;
        var shortName = seedName != null ? BotanyUiStyle.ShortSeedName(seedName) : "Unknown seed";

        if (seed == null)
            window.SetSeedBanner("EMPTY", "No seed packet loaded.", RMCMachineUiStyle.NoticeColor);
        else if (comp.SeedStats is { Viable: false })
            window.SetSeedBanner("NON-VIABLE", shortName, RMCMachineUiStyle.NoticeDangerColor);
        else
            window.SetSeedBanner("LOADED", shortName, RMCMachineUiStyle.NoticeSuccessColor);

        if (seed != null && comp.SeedStats is { } stats)
        {
            window.SetSeedChips(new (string, string, Color)[]
            {
                ("Yield", $"{stats.Yield}", RMCMachineUiStyle.GoodColor),
                ("Potency", $"{stats.Potency:0.#}", RMCMachineUiStyle.AverageColor),
                ("Endurance", $"{stats.Endurance:0.#}", RMCMachineUiStyle.BadColor),
            });

            window.SetSeedTraitRows(new (string, string, Color)[]
            {
                ("Lifespan", $"{stats.Lifespan:0.#}", Color.White),
                ("Maturation", $"{stats.Maturation:0.#}", Color.White),
                ("Production", $"{stats.Production:0.#}", Color.White),
                ("Harvest", BotanyUiStyle.GetHarvestLabel(stats.HarvestRepeat), Color.White),
                ("Seedless", stats.Seedless ? "Yes" : "No", Color.White),
                ("Viable", stats.Viable ? "Yes" : "No", stats.Viable ? RMCMachineUiStyle.GoodColor : RMCMachineUiStyle.BadColor),
            });
        }
        else
        {
            window.SetSeedChips([]);
            window.SetSeedTraitRows([]);
        }

        window.ProcessSeedButton.Disabled = seed == null || hasGenome;
        window.EjectSeedButton.Disabled = seed == null;

        var ratio = comp.MaxDegradation > 0 ? comp.Degradation / (float) comp.MaxDegradation : 0f;
        var degradationColor = ratio >= 0.75f
            ? RMCMachineUiStyle.BadColor
            : ratio >= 0.4f ? RMCMachineUiStyle.AverageColor : RMCMachineUiStyle.GoodColor;

        window.SetGenome(comp.GenomeName != null ? BotanyUiStyle.ShortSeedName(comp.GenomeName) : null, degradationColor);
        window.SetDegradation(ratio, hasGenome ? $"{comp.Degradation} / {comp.MaxDegradation}" : "—", degradationColor);
        window.ClearBufferButton.Disabled = !hasGenome;

        window.SetDisc(disc != null);
        window.EjectDiscButton.Disabled = disc == null;

        window.GenesCard.Visible = hasGenome;
        window.GeneRowsContainer.DisposeAllChildren();

        if (!hasGenome)
            return;

        var index = 0;
        foreach (var geneType in Enum.GetValues<PlantGeneType>())
        {
            var row = new LysisCentrifugeGeneRow();
            row.SetGene(
                index++,
                comp.ObfuscationCodes.GetValueOrDefault(geneType, "??"),
                geneType.GetLabel(),
                geneType.GetDescription(),
                disc == null ? "Insert a flora data disc first." : null);
            row.ExtractButton.Disabled = disc == null;
            row.ExtractButton.OnPressed += _ => SendPredictedMessage(new LysisCentrifugeExtractGeneBuiMsg(geneType));

            window.GeneRowsContainer.AddChild(row);
        }
    }
}
