using Content.Client._RMC14.Xenonids.UI;
using Content.Shared._RMC14.Xenonids.Construction;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
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

        _window = this.CreateWindow<XenoChooseStructureWindow>();
        _buttons.Clear();

        if (EntMan.TryGetComponent(Owner, out XenoConstructionComponent? xeno))
        {
            var hasBoost = EntMan.HasComponent<QueenBuildingBoostComponent>(Owner);

            foreach (var structureId in xeno.CanBuild)
            {
                if (!_prototype.TryIndex(structureId, out var structure))
                    continue;

                var control = new XenoChoiceControl();
                control.Button.ToggleMode = true;

                var displayId = structureId;
                var displayName = structure.Name;

                if (hasBoost)
                {
                    var queenVariant = GetQueenVariant(structureId);
                    if (_prototype.TryIndex(queenVariant, out var queenStructure) && queenVariant != structureId)
                    {
                        displayId = queenVariant;
                        displayName = queenStructure.Name;
                    }
                    displayName += " (0 plasma)";
                }
                else
                {
                    if (_xenoConstruction.GetStructurePlasmaCost(structureId) is { } cost)
                        displayName += $" ({cost} plasma)";
                }

                control.Set(displayName, _sprite.Frame0(_prototype.Index(displayId)));
                control.Button.OnPressed += _ =>
                {
                    SendPredictedMessage(new XenoChooseStructureBuiMsg(structureId));
                    UpdateButtonStates(structureId);
                };

                _window.StructureContainer.AddChild(control);
                _buttons.Add(structureId, control);
            }
        }

        Refresh();
    }

    private EntProtoId GetQueenVariant(EntProtoId originalId)
    {
        return originalId.Id switch
        {
            "WallXenoResin" => "WallXenoResinQueen",
            "WallXenoMembrane" => "WallXenoMembraneQueen",
            "DoorXenoResin" => "DoorXenoResinQueen",
            _ => originalId
        };
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

        if (EntMan.GetComponentOrNull<XenoConstructionComponent>(Owner)?.BuildChoice is { } choice &&
            _buttons.TryGetValue(choice, out var button))
        {
            button.Button.Pressed = true;
        }
    }
}
