using Content.Shared._RMC14.Xenonids.Construction.Tunnel;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._RMC14.Xenonids.Construction.Tunnel;

[UsedImplicitly]
public sealed partial class NameTunnelBui : BoundUserInterface
{
    private NameTunnelWindow? _window;
    public NameTunnelBui(EntityUid owner, Enum key) : base(owner, key)
    {

    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<NameTunnelWindow>();

        _window.OpenCentered();

        var tunnelInput = _window.TunnelName;

        _window.SubmitButton.OnPressed += (BaseButton.ButtonEventArgs args) =>
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
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        _window?.Dispose();
    }
}
