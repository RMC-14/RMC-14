using Content.Client._RMC14.Xenonids.UI;
using Content.Shared._RMC14.Xenonids.Destrain;
using Content.Shared._RMC14.Xenonids.Evolution;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Xenonids.Evolution;

[UsedImplicitly]
public sealed class XenoDevolveBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SpriteSystem _sprite;

    [ViewVariables]
    private XenoDevolveWindow? _window;

    public XenoDevolveBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sprite = EntMan.System<SpriteSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<XenoDevolveWindow>();
        if (!EntMan.TryGetComponent(Owner, out XenoDevolveComponent? xeno))
            return;

        foreach (var devolvesTo in xeno.DevolvesTo)
        {
            if (!_prototype.TryIndex(devolvesTo, out var evolution))
                return;

            var control = new XenoChoiceControl();
            control.Set(evolution.Name, _sprite.Frame0(evolution));

            control.Button.OnPressed += _ =>
            {
                SendPredictedMessage(new XenoDevolveBuiMsg(devolvesTo));
                Close();
            };

            _window.DevolutionsContainer.AddChild(control);
        }

        AddDestrain();
    }

    private void AddDestrain()
    {
        if (_window == null ||
            !EntMan.TryGetComponent(Owner, out XenoDestrainComponent? destrain) ||
            !_prototype.TryIndex(destrain.DestrainTo, out var baseCaste))
        {
            return;
        }

        var control = new XenoChoiceControl();
        var name = Loc.GetString("rmc-xeno-destrain-choice", ("caste", baseCaste.Name));
        control.Set(name, _sprite.Frame0(baseCaste));

        control.Button.OnPressed += _ =>
        {
            SendPredictedMessage(new XenoDestrainBuiMsg());
            Close();
        };

        _window.DevolutionsContainer.AddChild(control);
    }
}
