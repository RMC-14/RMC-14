using Content.Client._RMC14.Xenonids.UI;
using Content.Shared._RMC14.Xenonids.Evolution;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
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
        _window = new XenoDevolveWindow();
        _window.OnClose += Close;

        if (EntMan.TryGetComponent(Owner, out XenoDevolveComponent? xeno))
        {
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
        }

        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _window?.Dispose();
    }
}
