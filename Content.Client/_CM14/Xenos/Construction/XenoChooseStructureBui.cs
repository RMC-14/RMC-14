using Content.Client._CM14.Xenos.UI;
using Content.Shared._CM14.Xenos;
using Content.Shared._CM14.Xenos.Construction;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._CM14.Xenos.Construction;

[UsedImplicitly]
public sealed class XenoChooseStructureBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SpriteSystem _sprite;

    [ViewVariables]
    private XenoChooseStructureWindow? _window;

    public XenoChooseStructureBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sprite = EntMan.System<SpriteSystem>();
    }

    protected override void Open()
    {
        _window = new XenoChooseStructureWindow();
        _window.OnClose += Close;

        if (EntMan.TryGetComponent(Owner, out XenoComponent? xeno))
        {
            foreach (var structureId in xeno.CanBuild)
            {
                if (!_prototype.TryIndex(structureId, out var structure))
                    continue;

                var control = new XenoChoiceControl();
                control.Set(structure.Name, _sprite.Frame0(structure));

                control.Button.OnPressed += _ =>
                {
                    SendMessage(new XenoChooseStructureBuiMessage(structureId));
                    Close();
                };

                _window.StructureContainer.AddChild(control);
            }
        }

        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _window?.Dispose();
    }
}
