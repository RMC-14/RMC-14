using Content.Client._RMC14.Xenonids.UI;
using Content.Shared._RMC14.Xenonids.Construction;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Xenonids.Construction;

[UsedImplicitly]
public sealed class XenoOrderConstructionBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SpriteSystem _sprite;

    private readonly Dictionary<EntProtoId, XenoChoiceControl> _buttons = new();

    [ViewVariables]
    private XenoChooseStructureWindow? _window;

    public XenoOrderConstructionBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sprite = EntMan.System<SpriteSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<XenoChooseStructureWindow>();
        _window.Title = Loc.GetString("cm-xeno-order-construction");
        _buttons.Clear();

        if (EntMan.TryGetComponent(Owner, out XenoConstructionComponent? xeno))
        {
            foreach (var structureId in xeno.CanOrderConstruction)
            {
                if (!_prototype.TryIndex(structureId, out var structure))
                    continue;

                var control = new XenoChoiceControl();
                control.Button.ToggleMode = true;

                control.Set(structure.Name, _sprite.Frame0(structure));

                control.Button.OnPressed += _ =>
                {
                    SendPredictedMessage(new XenoOrderConstructionBuiMsg(structureId));
                    UpdateButtonStates(structureId);
                };

                _window.StructureContainer.AddChild(control);
                _buttons.Add(structureId, control);
            }
        }

        Refresh();
    }

    private void UpdateButtonStates(EntProtoId selectedId)
    {
        foreach (var (structureId, control) in _buttons)
        {
            control.Button.Pressed = (structureId == selectedId);
        }
    }

    public void Refresh()
    {
        foreach (var (_, control) in _buttons)
        {
            control.Button.Pressed = false;
        }

        if (EntMan.GetComponentOrNull<XenoConstructionComponent>(Owner)?.OrderConstructionChoice is { } choice &&
            _buttons.TryGetValue(choice, out var button))
        {
            button.Button.Pressed = true;
        }
    }
}
