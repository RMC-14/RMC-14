using System.Linq;
using Content.Client._RMC14.UserInterface;
using Content.Client._RMC14.Xenonids.UI;
using Content.Shared._RMC14.Xenonids.Construction;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Xenonids.Construction;

[UsedImplicitly]
public sealed class XenoOrderConstructionBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SpriteSystem _sprite;

    private readonly Dictionary<EntProtoId, XenoChoiceControl> _buttons = new();
    private readonly List<EntProtoId> _structureIds = new();

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

        Refresh();
    }

    public void Refresh()
    {
        if (_window == null)
            return;

        _buttons.Clear();
        _structureIds.Clear();
        // set up all buttons and create more if necessary
        if (EntMan.TryGetComponent(Owner, out XenoConstructionComponent? xeno))
        {
            foreach (var (structureId, index) in xeno.CanOrderConstruction.Select((structureId, index) => (structureId, index)))
            {
                if (!_prototype.TryIndex(structureId, out var structure))
                    continue;

                XenoChoiceControl? control;

                if (index < _window.StructureContainer.ChildCount)
                {
                    control = _window.StructureContainer.GetChild(index) as XenoChoiceControl;
                }
                else
                {
                    control = new XenoChoiceControl();
                    control.Button.OnPressed += _ =>
                    {
                        if (index < _structureIds.Count)
                        {
                            SendPredictedMessage(new XenoOrderConstructionBuiMsg(_structureIds[index]));
                            Close();
                        }
                    };
                    control.Button.ToggleMode = false;
                    _window.StructureContainer.AddChild(control);
                }

                if (control == null)
                    continue;

                var displayId = structureId;
                var displayName = structure.Name;

                control.Set(displayName, _sprite.Frame0(structure));

                _structureIds.Add(structureId);
                _buttons.Add(structureId, control);
            }

            // If we have too many buttons, delete the extras
            _window.StructureContainer.RemoveChildrenAfter(xeno.CanOrderConstruction.Count);
        }
        else
        {
            // there is nothing that can be built, remove all the items
            _window.StructureContainer.RemoveAllChildren();
        }
    }
}
