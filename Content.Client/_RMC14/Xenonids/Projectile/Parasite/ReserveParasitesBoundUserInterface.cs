using Content.Shared._RMC14.Xenonids.Projectile.Parasite;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Xenonids.Projectile.Parasite;

[UsedImplicitly]
public sealed class ReserveParasitesBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private ReserveParasitesWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ReserveParasitesWindow>();
        if (EntMan.TryGetComponent<XenoParasiteThrowerComponent>(Owner, out var paras))
            _window.SetReserveShown(paras.ReservedParasites);

        _window.ApplyButton.OnPressed += _ =>
        {
            SendMessage(new XenoChangeParasiteReserveMessage(_window.ReserveBar.Value));
            _window.Close();
        };
    }

    public void ChangeReserve(int newReserve)
    {
        SendMessage(new XenoChangeParasiteReserveMessage(newReserve));
    }
}
