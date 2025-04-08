using Content.Client._RMC14.Xenonids.UI;
using Content.Shared._RMC14.Xenonids.Fruit;
using Content.Shared._RMC14.Xenonids.Fruit.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Xenonids.Fruit;


[UsedImplicitly]
public sealed class XenoFruitChooseBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SpriteSystem _sprite;
    private readonly SharedXenoFruitSystem _xenoFruit;

    private readonly Dictionary<EntProtoId, XenoChoiceControl> _buttons = new();

    [ViewVariables]
    private XenoFruitChooseWindow? _window;

    public XenoFruitChooseBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sprite = EntMan.System<SpriteSystem>();
        _xenoFruit = EntMan.System<SharedXenoFruitSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _window = new XenoFruitChooseWindow();
        _window.OnClose += Close;

        _buttons.Clear();
        var group = new ButtonGroup();
        if (EntMan.TryGetComponent(Owner, out XenoFruitPlanterComponent? xeno))
        {
            _window.FruitCountLabel.Text = Loc.GetString("rmc-xeno-fruit-ui-count", ("count", xeno.PlantedFruit.Count), ("max", xeno.MaxFruitAllowed));

            foreach (var fruitId in xeno.CanPlant)
            {
                if (!_prototype.TryIndex(fruitId, out var fruit))
                    continue;

                var sprite = _xenoFruit.GetFruitSprite(fruit);

                var control = new XenoChoiceControl();
                control.Button.Group = group;

                var name = fruit.Name;

                var specifier = new SpriteSpecifier.Rsi(new ResPath("_RMC14/Structures/Xenos/xeno_fruit.rsi"), sprite);
                control.Set(name, _sprite.Frame0(specifier));
                control.Button.OnPressed += _ => SendPredictedMessage(new XenoFruitChooseBuiMsg(fruitId));
                control.Button.ToolTip = fruit.Description;
                control.Button.TooltipDelay = 0.1f;

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

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is XenoFruitChooseBuiState uiState)
            UpdateState(uiState);
    }

    private void UpdateState(XenoFruitChooseBuiState state)
    {
        if (_window == null)
            return;

        _window.FruitCountLabel.Text = Loc.GetString("rmc-xeno-fruit-ui-count", ("count", state.Count), ("max", state.Max));
    }

    public void Refresh()
    {
        if (!EntMan.TryGetComponent(Owner, out XenoFruitPlanterComponent? xeno) ||
            _window == null)
            return;

        if (xeno.FruitChoice is not { } choice ||
            !_buttons.TryGetValue(choice, out var button))
            return;

        button.Button.Pressed = true;
    }
}
