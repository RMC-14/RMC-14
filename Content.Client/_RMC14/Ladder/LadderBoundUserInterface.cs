using Content.Client.UserInterface.Controls;
using Content.Shared._RMC14.Ladder;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Ladder;

[UsedImplicitly]
public sealed class LadderBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private static readonly Dictionary<RadialDirection, SpriteSpecifier> ButtonSprites = new()
    {
        { RadialDirection.Up, new SpriteSpecifier.Rsi(new ResPath("_RMC14/Interface/radial.rsi"), "radial_up") },
        { RadialDirection.Down, new SpriteSpecifier.Rsi(new ResPath("_RMC14/Interface/radial.rsi"), "radial_down") }
    };

    private SimpleRadialMenu? _menu;

    protected override void Open()
    {
        if (!EntMan.TryGetComponent<LadderComponent>(Owner, out var ladderComp)
            || ladderComp.Connected.Count < 2)
        {
            return;
        }

        base.Open();

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);
        _menu.SetButtons(CreateButtons(ladderComp), new SimpleRadialMenuSettings
        {
            UseSectors = false
        });
    }

    private List<RadialMenuActionOption> CreateButtons(LadderComponent ladderComp)
    {
        var buttons = new List<RadialMenuActionOption>();

        var ownerDirection = ladderComp.Direction;
        foreach (var connected in ladderComp.Connected)
        {
            if (!EntMan.TryGetComponent<LadderComponent>(connected, out var connectedLadder))
                continue;

            // this is a bit hacky but it works
            var relativeDirection = connectedLadder.Direction > ownerDirection ? RadialDirection.Up : RadialDirection.Down;
            var buttonIcon = ButtonSprites[relativeDirection];

            var button = new RadialMenuActionOption<NetEntity>(SelectDirection, EntMan.GetNetEntity(connected))
            {
                Sprite = buttonIcon
            };
            buttons.Add(button);
        }

        return buttons;
    }

    private void SelectDirection(NetEntity selected)
    {
        var message = new RadialLadderSelectedMessage(selected);
        SendPredictedMessage(message);
    }
}

public enum RadialDirection
{
    Up,
    Down
}
