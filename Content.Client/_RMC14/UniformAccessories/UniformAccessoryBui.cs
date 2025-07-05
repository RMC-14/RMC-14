using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared._RMC14.UniformAccessories;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Containers;

namespace Content.Client._RMC14.UniformAccessories;

[UsedImplicitly]
public sealed class UniformAccessoryBui : BoundUserInterface
{
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IEyeManager _eye = default!;

    private readonly TransformSystem _transform;
    private readonly SharedContainerSystem _container;

    private UniformAccessoryMenu? _menu;

    public UniformAccessoryBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);

        _transform = EntMan.System<TransformSystem>();
        _container = EntMan.System<SharedContainerSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<UniformAccessoryMenu>();

        if (EntMan.Deleted(Owner))
            return;

        Refresh();
    }

    public void Refresh()
    {
        if (_menu == null)
            return;

        if (!EntMan.TryGetComponent(Owner, out UniformAccessoryHolderComponent? holderComponent))
            return;

        if (!_container.TryGetContainer(Owner, holderComponent.ContainerId, out var container))
            return;

        _menu?.Accessories.Children.Clear();

        foreach (var accessory in container.ContainedEntities)
        {
            if (!EntMan.TryGetComponent(accessory, out MetaDataComponent? metaData))
                continue;

            var button = new RadialMenuTextureButton
            {
                StyleClasses = { "RadialMenuButton" },
                SetSize = new Vector2(64, 64),
                ToolTip = metaData.EntityName,
            };

            var spriteView = new SpriteView
            {
                OverrideDirection = Direction.South,
                Scale = new Vector2(2f, 2f),
                MaxSize = new Vector2(112, 112),
                Stretch = SpriteView.StretchMode.Fill,
            };

            spriteView.SetEntity(accessory);

            var netEnt = EntMan.GetNetEntity(accessory);
            button.OnButtonDown += _ => SendPredictedMessage(new UniformAccessoriesBuiMsg(netEnt));

            button.AddChild(spriteView);
            _menu?.Accessories.AddChild(button);
        }

        var vpSize = _displayManager.ScreenSize;
        var pos = _eye.WorldToScreen(_transform.GetMapCoordinates(Owner).Position) / vpSize;
        _menu?.OpenCenteredAt(pos);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (State is UniformAccessoriesBuiState s)
            Refresh();
    }
}
