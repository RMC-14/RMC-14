using Content.Shared._RMC14.Xenonids.Projectile.Parasite;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Xenonids.Projectiles.Parasite;

[UsedImplicitly]
public sealed class ReserveParasitesBoundUserInterface : BoundUserInterface
{
    private IEntityManager _entManager;
    private EntityUid _owner;

    [ViewVariables]
    private ReserveParasitesWindow? _window;

    public ReserveParasitesBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _owner = owner;
        _entManager = IoCManager.Resolve<IEntityManager>();
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<ReserveParasitesWindow>();

        if (_entManager.TryGetComponent<XenoParasiteThrowerComponent>(_owner, out var paras))
            _window.SetReserveShown(paras.ReservedParasites);

        _window.ApplyButton.OnPressed += _ =>
        {
            SendMessage(new XenoChangeParasiteReserveMessage(_window.ReserveBar.Value));
            _window.Close();
        };


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
        SendMessage(new XenoChangeParasiteReserveMessage(newReserve));
    }
}
