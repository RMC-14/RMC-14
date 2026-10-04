using Content.Client._RMC14.Xenonids.UI;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Prototypes;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Xenonids.Construction;

[UsedImplicitly]
public sealed class XenoOrderConstructionBui : BoundUserInterface
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SharedXenoHiveSystem _hive;
    private readonly SpriteSystem _sprite;

    private readonly Dictionary<EntProtoId, XenoChoiceControl> _buttons = new();

    [ViewVariables]
    private XenoChooseStructureWindow? _window;

    public XenoOrderConstructionBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _hive = EntMan.System<SharedXenoHiveSystem>();
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
            var hiveColor = _hive.GetMemberColor(Owner);

            foreach (var structureId in xeno.CanOrderConstruction)
            {
                if (!_prototype.TryIndex(structureId, out var structure))
                    continue;

                var control = new XenoChoiceControl();
                control.Button.ToggleMode = false;

                control.Set(structure.Name, _sprite.Frame0(structure));

                if (structure.HasComponent<HiveColoredComponent>(_compFactory))
                    control.Texture.ModulateSelfOverride = hiveColor;

                control.Button.OnPressed += _ =>
                {
                    SendPredictedMessage(new XenoOrderConstructionBuiMsg(structureId));
                    Close();
                };

                _window.StructureContainer.AddChild(control);
                _buttons.Add(structureId, control);
            }
        }
    }
}
