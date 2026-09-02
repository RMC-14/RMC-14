using Content.Client.UserInterface.Controls;
using Content.Shared._RMC14.Ladder;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Client._RMC14.Ladder;

[UsedImplicitly]
public sealed class LadderBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SimpleRadialMenu? _menu;

    private SelectionReason? _reason;

    protected override void Open()
    {
        base.Open();
        EnsureWindow();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not LadderRadialBuiState ladderState)
            return;

        _reason = ladderState.Reason;
        var window = EnsureWindow();

        var actionString = _reason switch
        {
            SelectionReason.Climb => Loc.GetString("rmc-ladder-radial-action-climb"),
            SelectionReason.Watch => Loc.GetString("rmc-ladder-radial-action-look"),
            _ => null,
        };

        var buttons = new List<RadialMenuActionOption>();
        if (ladderState.Above is { } ladderAbove)
        {
            var upButton = new RadialMenuActionOption<NetEntity>(SelectDirection, ladderAbove)
            {
                Sprite = new SpriteSpecifier.Rsi(new ResPath("_RMC14/Interface/radial.rsi"), "radial_up"),
                ToolTip = actionString == null ? null : Loc.GetString("rmc-ladder-radial-tooltip-up", ("action", actionString))
            };
            buttons.Add(upButton);
        }
        if (ladderState.Below is { } ladderBelow)
        {
            var downButton = new RadialMenuActionOption<NetEntity>(SelectDirection, ladderBelow)
            {
                Sprite = new SpriteSpecifier.Rsi(new ResPath("_RMC14/Interface/radial.rsi"), "radial_down"),
                ToolTip = actionString == null ? null : Loc.GetString("rmc-ladder-radial-tooltip-down", ("action", actionString))
            };
            buttons.Add(downButton);
        }

        window.SetButtons(buttons, new SimpleRadialMenuSettings()
        {
            UseSectors = false
        });

        // Styling override thing because `SimpleRadialMenu` doesn't let you do this in a more "normal" way.
        foreach (var child in window.Children)
        {
            if (child is not RadialContainer container)
                continue;

            container.RadialAlignment = RadialContainer.RAlignment.AntiClockwise;
            container.AngularRange = new Vector2(MathF.Tau / 4, MathF.Tau * 3 / 4);

            foreach (var button in container.Children)
            {
                button.AddStyleClass("RadialMenuButton");
                // Todo: this doesn't actually work yet :(
            }
            break;
        }
    }

    private SimpleRadialMenu EnsureWindow()
    {
        if (_menu != null)
            return _menu;

        _menu ??= this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);
        return _menu;
    }

    private void SelectDirection(NetEntity selectedLadder)
    {
        if (_reason is { } selectionReason)
        {
            var message = new LadderRadialSelectedMessage(selectedLadder, selectionReason);
            SendPredictedMessage(message);
        }
    }
}
