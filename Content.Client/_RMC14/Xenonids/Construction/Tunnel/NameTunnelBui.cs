using Content.Shared._RMC14.Xenonids.Construction.Tunnel;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Xenonids.Construction.Tunnel;

[UsedImplicitly]
public sealed partial class NameTunnelBui(EntityUid owner, Enum key) : BoundUserInterface(owner, key)
{
    [ViewVariables]
    private NameTunnelWindow? _window;

    protected override void Open()
    {
        base.Open();

        // See https://github.com/space-wizards/RobustToolbox/issues/6005
        if (_window is { IsOpen: true })
            return;

        _window = this.CreateWindow<NameTunnelWindow>();

        var tunnelInput = _window.TunnelName;

        _window.SubmitButton.OnPressed += _ =>
        {
            var tunnelName = tunnelInput.Text.Trim();
            if (tunnelName.Length == 0)
            {
                return;
            }
            SendMessage(new NameTunnelMessage(tunnelName));
            // If tunnel naming suceeds, the server shuts down the ui
        };
    }
}
