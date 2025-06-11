using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared._RMC14.Construction.Upgrades;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Construction.Upgrades;

[UsedImplicitly]
public sealed class RMCConstructionUpgradeBui : BoundUserInterface
{
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private readonly SpriteSystem _sprite;
    private readonly TransformSystem _transform;

    private RMCConstructionUpgradeMenu? _menu;

    public RMCConstructionUpgradeBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);

        _sprite = EntMan.System<SpriteSystem>();
        _transform = EntMan.System<TransformSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<RMCConstructionUpgradeMenu>();

        if (EntMan.TryGetComponent(Owner, out RMCConstructionUpgradeTargetComponent? upgradeComp) &&
            upgradeComp.Upgrades is { } upgrades)
        {
            foreach (var upgradeId in upgrades)
            {
                if (!_prototypes.TryIndex(upgradeId, out var upgradeProto))
                    continue;

                var button = new RadialMenuTextureButton
                {
                    StyleClasses = { "RadialMenuButton" },
                    SetSize = new Vector2(64, 64),
                    ToolTip = upgradeProto.Name,
                };

                var texture = new TextureRect
                {
                    VerticalAlignment = Control.VAlignment.Center,
                    HorizontalAlignment = Control.HAlignment.Center,
                    Texture = _sprite.GetPrototypeIcon(upgradeProto).GetFrame(RsiDirection.South, 0),
                    TextureScale = new Vector2(2f, 2f),
                };

                button.OnButtonDown += _ => SendPredictedMessage(new RMCConstructionUpgradeBuiMsg(upgradeId));

                button.AddChild(texture);
                _menu.Upgrades.AddChild(button);
            }
        }

        if (EntMan.Deleted(Owner))
            return;

        var vpSize = _displayManager.ScreenSize;
        var pos = _eye.WorldToScreen(_transform.GetMapCoordinates(Owner).Position) / vpSize;

        _menu.OpenCenteredAt(pos);
    }
}
