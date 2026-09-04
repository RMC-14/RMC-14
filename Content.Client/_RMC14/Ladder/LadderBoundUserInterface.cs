using Content.Client.UserInterface.Controls;
using Content.Shared._RMC14.Ladder;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using System.Linq;
using System.Numerics;

namespace Content.Client._RMC14.Ladder;

[UsedImplicitly]
public sealed class LadderBoundUserInterface : BoundUserInterface
{
    private readonly SpriteSystem _sprite;

    private SimpleRadialMenu? _menu;

    private SelectionReason? _reason;

    private static readonly SpriteSpecifier UpIcon = new SpriteSpecifier.Rsi(new ResPath("_RMC14/Interface/radial.rsi"), "radial_up");
    private static readonly SpriteSpecifier DownIcon = new SpriteSpecifier.Rsi(new ResPath("_RMC14/Interface/radial.rsi"), "radial_down");

    public LadderBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sprite = EntMan.System<SpriteSystem>();
    }

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

        string? upTooltip = null;
        string? downTooltip = null;
        var actionString = _reason switch
        {
            SelectionReason.Climb => Loc.GetString("rmc-ladder-radial-action-climb"),
            SelectionReason.Watch => Loc.GetString("rmc-ladder-radial-action-look"),
            _ => null,
        };
        if (actionString != null)
        {
            upTooltip = Loc.GetString("rmc-ladder-radial-tooltip",
                ("action", actionString),
                ("direction", Loc.GetString("rmc-ladder-direction-up")));

            downTooltip = Loc.GetString("rmc-ladder-radial-tooltip",
                ("action", actionString),
                ("direction", Loc.GetString("rmc-ladder-direction-down")));
        }

        var buttons = new List<RadialMenuActionOption>();
        var upButton = new RadialMenuActionOption<NetEntity>(SelectDirection, ladderState.Above)
        {
            ToolTip = upTooltip
        };
        buttons.Add(upButton);

        var downButton = new RadialMenuActionOption<NetEntity>(SelectDirection, ladderState.Below)
        {
            ToolTip = downTooltip
        };
        buttons.Add(downButton);

        window.SetButtons(buttons, new SimpleRadialMenuSettings()
        {
            UseSectors = false
        });

        // Styling override thing because `SimpleRadialMenu` doesn't let you do this in a more "normal" way.
        foreach (var child in window.Children)
        {
            if (child is not RadialContainer container)
                continue;

            // Position the buttons at the top and bottom of the radial menu.
            container.RadialAlignment = RadialContainer.RAlignment.AntiClockwise;
            container.AngularRange = new Vector2(MathF.Tau / 4, MathF.Tau * 3 / 4);

            if (container.ChildCount < 2)
                return;

            // In order of the buttons' creation above.
            AddButtonIcon(container.Children.ElementAt(0), UpIcon);
            AddButtonIcon(container.Children.ElementAt(1), DownIcon);
            break;
        }
    }

    // Workaround until `RadialMenuIconSpecifier` is ported from upstream.
    //
    // Basically, radial menu buttons can only have a background (added through the "RadialMenuButton" style)
    // if their icon is set as a TextureRect child of the button, for various confusing reasons.
    // The current implementation of `SimpleRadialMenu` sets the icon as the button's `TextureNormal`,
    // so this is to get around that.
    // (The wizden PR adding `RadialMenuIconSpecifier` seems to solve this, so this can all be changed when that gets ported)
    private void AddButtonIcon(Control button, SpriteSpecifier icon)
    {
        var actualButtonSprite = new TextureRect
        {
            VerticalAlignment = Control.VAlignment.Center,
            HorizontalAlignment = Control.HAlignment.Center,
            Texture = _sprite.Frame0(icon),
            TextureScale = new Vector2(2f, 2f)
        };
        button.AddChild(actualButtonSprite);
        button.AddStyleClass("RadialMenuButton");
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
