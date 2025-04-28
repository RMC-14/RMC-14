using Content.Shared._RMC14.Medical.HUD;
using JetBrains.Annotations;
using Robust.Client.Player;

namespace Content.Client._RMC14.Medical.HUD.Holocard;

[UsedImplicitly]
public sealed class HolocardChangeBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEntityManager _entities = default!;

    private HolocardChangeWindow? _window;

    public HolocardChangeBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();
        _window = new HolocardChangeWindow(this);
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _window?.Dispose();
    }

    public void ChangeHolocard(HolocardStatus newHolocardStatus)
    {
        if (_entities.GetNetEntity(_player.LocalEntity) is { } viewer)
            SendMessage(new HolocardChangeEvent(viewer, newHolocardStatus));
    }
}
