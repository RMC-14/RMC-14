using Content.Client.UserInterface.Controls;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Pheromones;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Client._RMC14.Xenonids.Pheromones;

[UsedImplicitly]
public sealed class XenoPheromonesBui : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;

    private readonly SpriteSystem _sprite;

    [ViewVariables]
    private XenoPheromonesMenu? _xenoPheromonesMenu;

    public XenoPheromonesBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);

        _sprite = _entities.System<SpriteSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _xenoPheromonesMenu = new XenoPheromonesMenu();
        _xenoPheromonesMenu.OnClose += Close;

        var parent = _xenoPheromonesMenu.FindControl<RadialContainer>("Main");

        if (EntMan.HasComponent<XenoComponent>(Owner))
        {
            foreach (var pheromones in Enum.GetValues<XenoPheromones>())
            {
                var name = pheromones.ToString().ToLowerInvariant();

                var texture = new TextureRect
                {
                    VerticalAlignment = Control.VAlignment.Center,
                    HorizontalAlignment = Control.HAlignment.Center,
                    Texture = _sprite.Frame0(new SpriteSpecifier.Rsi(new ResPath("/Textures/_RMC14/Interface/xeno_pheromones.rsi"), name)),
                    TextureScale = new Vector2(2f, 2f),
                };

                var button = new RadialMenuTextureButton()
                {
                    StyleClasses = { "RadialMenuButton" },
                    SetSize = new Vector2(64, 64),
                    ToolTip = name,
                };

                button.OnButtonDown += _ => SendPredictedMessage(new XenoPheromonesChosenBuiMsg(pheromones));

                button.AddChild(texture);
                parent.AddChild(button);
            }
        }

        var vpSize = _displayManager.ScreenSize;
        _xenoPheromonesMenu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _xenoPheromonesMenu?.Dispose();
    }
}
