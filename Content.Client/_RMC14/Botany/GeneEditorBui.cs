using Content.Client._RMC14.UserInterface;
using Content.Shared._RMC14.Botany;
using Content.Shared._RMC14.UserInterface;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Maths;

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
        _window.Title = EntMan.GetComponentOrNull<MetaDataComponent>(Owner)?.EntityName ?? string.Empty;

        _window.ApplyButton.OnPressed += _ => SendPredictedMessage(new GeneEditorApplyBuiMsg());
        _window.EjectButton.OnPressed += _ => SendPredictedMessage(new GeneEditorEjectBuiMsg());

        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { } window ||
            !EntMan.TryGetComponent(Owner, out GeneEditorComponent? comp))
        {
            return;
        }

        var itemSlots = EntMan.System<ItemSlotsSystem>();
        var hasDisc = itemSlots.GetItemOrNull(Owner, comp.DiscSlotId) != null;
        var seed = itemSlots.GetItemOrNull(Owner, comp.SeedSlotId);

        window.SeedSpriteView.Visible = seed != null;
        window.SeedSpriteView.SetEntity(seed);

        var seedName = seed != null ? EntMan.GetComponentOrNull<MetaDataComponent>(seed.Value)?.EntityName : null;
        var shortName = seedName != null ? BotanyUiStyle.ShortSeedName(seedName) : "Unknown seed";
        var editRatio = comp.MaxEditCount > 0 ? comp.SeedEditCount / (float) comp.MaxEditCount : 0f;

        if (seed == null)
            window.SetSeedBanner("EMPTY", "No seed packet loaded.", RMCMachineUiStyle.NoticeColor);
        else if (comp.SeedStats is { Viable: false })
            window.SetSeedBanner("NON-VIABLE", shortName, RMCMachineUiStyle.NoticeDangerColor);
        else if (editRatio >= 1f)
            window.SetSeedBanner("MAXED", shortName, RMCMachineUiStyle.NoticeWarningColor);
        else
            window.SetSeedBanner("LOADED", shortName, RMCMachineUiStyle.NoticeSuccessColor);

        if (seed != null)
        {
            var editColor = editRatio >= 0.75f
                ? RMCMachineUiStyle.BadColor
                : editRatio >= 0.4f ? RMCMachineUiStyle.AverageColor : RMCMachineUiStyle.GoodColor;
            window.SetEditCount(editRatio, $"{comp.SeedEditCount} / {comp.MaxEditCount}", editColor);
        }
        else
        {
            window.SetEditCount(null, string.Empty, RMCMachineUiStyle.GoodColor);
        }

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

        window.ApplyButton.Disabled = seed == null || !hasDisc || comp.DiscGeneLabels.Count == 0;
        window.EjectButton.Disabled = seed == null && !hasDisc;

        window.SetDisc(hasDisc, comp.DiscSource, comp.DiscGeneLabels);
    }
}
