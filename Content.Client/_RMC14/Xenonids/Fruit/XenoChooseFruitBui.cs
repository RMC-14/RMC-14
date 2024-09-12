using Content.Client._RMC14.Xenonids.UI;
using Content.Shared._RMC14.Xenonids.Fruit;
using Content.Shared._RMC14.Xenonids.Fruit.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Xenonids.Fruit;


[UsedImplicitly]
public sealed class XenoChooseFruitBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SpriteSystem _sprite;
    private readonly SharedXenoFruitSystem _xenoFruit;

    private readonly Dictionary<EntProtoId, XenoChoiceControl> _buttons = new();

    [ViewVariables]
    private XenoChooseFruitWindow? _window;

    public XenoChooseFruitBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sprite = EntMan.System<SpriteSystem>();
        _xenoFruit = EntMan.System<SharedXenoFruitSystem>();
    }

    protected override void Open()
    {
        _window = new XenoChooseFruitWindow();
        _window.OnClose += Close;

        _buttons.Clear();
        var group = new ButtonGroup();
        if (EntMan.TryGetComponent(Owner, out XenoFruitPlanterComponent? xeno))
        {
            foreach (var fruitId in xeno.CanPlant)
            {
                if (!_prototype.TryIndex(fruitId, out var fruit))
                    continue;

                var control = new XenoChoiceControl();
                control.Button.Group = group;

                var name = fruit.Name;

                control.Set(name, _sprite.Frame0(fruit));
                control.Button.OnPressed += _ => SendPredictedMessage(new XenoChooseFruitBuiMsg(fruitId));

                _window.FruitContainer.AddChild(control);
                _buttons.Add(fruitId, control);
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
        if (EntMan.GetComponentOrNull<XenoFruitPlanterComponent>(Owner)?.FruitChoice is not { } choice ||
            !_buttons.TryGetValue(choice, out var button))
        {
            return;
        }

        button.Button.Pressed = true;
    }
}
