using Content.Shared._RMC14.Radio;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Radio;

[UsedImplicitly]
public sealed class RMCRadioFilterBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    [ViewVariables]
    private RMCRadioFilterWindow? _window;

    public RMCRadioFilterBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<RMCRadioFilterWindow>();

        Refresh();
    }

    public void Refresh()
    {
        if (_window == null)
            return;

        if (!EntMan.TryGetComponent(Owner, out RMCRadioFilterComponent? comp))
            return;

        if (!EntMan.TryGetComponent(Owner, out EncryptionKeyHolderComponent? holder))
            return;

        foreach (var channel in holder.Channels)
        {
            if (!_prototype.TryIndex<RadioChannelPrototype>(channel, out var proto))
                continue;

            var checkbox = new CheckBox
            {
                Text = Loc.GetString(proto.Name),
                Pressed = !comp.DisabledChannels.Contains(channel)
            };

            checkbox.OnToggled += args =>
            {
                SendPredictedMessage(new RMCRadioFilterBuiMsg(channel, args.Pressed));
            };

            _window.CheckboxContainer.AddChild(checkbox);
        }
    }
}
