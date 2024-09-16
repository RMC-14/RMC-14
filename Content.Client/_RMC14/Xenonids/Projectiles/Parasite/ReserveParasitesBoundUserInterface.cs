using Content.Shared._RMC14.Xenonids.Projectile.Parasite;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._RMC14.Xenonids.Projectiles.Parasite;

[UsedImplicitly]
public sealed partial class ReserveParasitesBoundUserInterface : BoundUserInterface
{
    private ReserveParasitesWindow? _window;
    public ReserveParasitesBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = new ReserveParasitesWindow(this);

        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        _window?.Dispose();
    }

    public void ChangeReserve(int newReserve)
    {
        SendMessage(new XenoChangeParasiteReserveEvent(newReserve));
    }
}
