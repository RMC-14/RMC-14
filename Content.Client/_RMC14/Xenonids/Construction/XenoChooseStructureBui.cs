using Content.Client._RMC14.Xenonids.UI;
using Content.Shared._RMC14.Xenonids.Construction;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Xenonids.Construction;

[UsedImplicitly]
public sealed class XenoChooseStructureBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SpriteSystem _sprite;
    private readonly SharedXenoConstructionSystem _xenoConstruction;

    private readonly Dictionary<EntProtoId, XenoChoiceControl> _buttons = new();

    [ViewVariables]
    private XenoChooseStructureWindow? _window;

    public XenoChooseStructureBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sprite = EntMan.System<SpriteSystem>();
        _xenoConstruction = EntMan.System<SharedXenoConstructionSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _window = new XenoChooseStructureWindow();
        _window.OnClose += Close;

        _buttons.Clear();
        var group = new ButtonGroup();
        if (EntMan.TryGetComponent(Owner, out XenoConstructionComponent? xeno))
        {
            foreach (var structureId in xeno.CanBuild)
            {
                if (!_prototype.TryIndex(structureId, out var structure))
                    continue;

                var control = new XenoChoiceControl();
                control.Button.Group = group;
                control.Button.Mode = 0;

                var name = structure.Name;
                if (_xenoConstruction.GetStructurePlasmaCost(structureId) is { } cost)
                    name += $" ({cost} plasma)";

                control.Set(name, _sprite.Frame0(structure));
                control.Button.OnPressed += _ => SendPredictedMessage(new XenoChooseStructureBuiMsg(structureId));

                _window.StructureContainer.AddChild(control);
                _buttons.Add(structureId, control);
            }
        }

        Refresh();
        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _window?.Dispose();
    }

    public void Refresh()
    {
        if (EntMan.GetComponentOrNull<XenoConstructionComponent>(Owner)?.BuildChoice is not { } choice ||
            !_buttons.TryGetValue(choice, out var button))
        {
            return;
        }

        button.Button.Pressed = true;
    }
}
