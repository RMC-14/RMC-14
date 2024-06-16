using Content.Client._CM14.Xenos.UI;
using Content.Shared._CM14.Xenos.Construction;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._CM14.Xenos.Construction;

[UsedImplicitly]
public sealed class XenoOrderConstructionBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SpriteSystem _sprite;

    [ViewVariables]
    private XenoChooseStructureWindow? _window;

    public XenoOrderConstructionBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sprite = EntMan.System<SpriteSystem>();
    }

    protected override void Open()
    {
        _window = new XenoChooseStructureWindow();
        _window.OnClose += Close;

        _window.Title = Loc.GetString("cm-xeno-order-construction");

        if (EntMan.TryGetComponent(Owner, out XenoConstructionComponent? xeno))
        {
            foreach (var structureId in xeno.CanOrderConstruction)
            {
                if (!_prototype.TryIndex(structureId, out var structure))
                    continue;

                var control = new XenoChoiceControl();
                control.Set(structure.Name, _sprite.Frame0(structure));

                control.Button.OnPressed += _ =>
                {
                    SendPredictedMessage(new XenoOrderConstructionBuiMsg(structureId));
                };

                _window.StructureContainer.AddChild(control);
            }
        }

        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Dispose();
    }
}
