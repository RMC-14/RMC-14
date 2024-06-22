using Content.Client._CM14.Xenonids.UI;
using Content.Shared._CM14.Xenonids.Construction;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._CM14.Xenonids.Construction;

[UsedImplicitly]
public sealed class XenoChooseStructureBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SpriteSystem _sprite;
    private readonly SharedXenoConstructionSystem _xenoConstruction;

    [ViewVariables]
    private XenoChooseStructureWindow? _window;

    public XenoChooseStructureBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sprite = EntMan.System<SpriteSystem>();
        _xenoConstruction = EntMan.System<SharedXenoConstructionSystem>();
    }

    protected override void Open()
    {
        _window = new XenoChooseStructureWindow();
        _window.OnClose += Close;

        if (EntMan.TryGetComponent(Owner, out XenoConstructionComponent? xeno))
        {
            foreach (var structureId in xeno.CanBuild)
            {
                if (!_prototype.TryIndex(structureId, out var structure))
                    continue;

                var control = new XenoChoiceControl();
                var name = structure.Name;
                if (_xenoConstruction.GetStructurePlasmaCost(structureId) is { } cost)
                    name += $" ({cost} plasma)";

                control.Set(name, _sprite.Frame0(structure));
                control.Button.OnPressed += _ => SendPredictedMessage(new XenoChooseStructureBuiMsg(structureId));

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
