using Content.Client._RMC14.Camera;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Photo;

[UsedImplicitly]
public sealed partial class PhotoBui : BoundUserInterface
{
    private readonly RMCPhotoCameraSystem _photo;

    private PhotoWindow? _window;

    public PhotoBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _photo = EntMan.System<RMCPhotoCameraSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<PhotoWindow>();

        Refresh();
    }

    public void Refresh()
    {
        if (_window == null)
            return;

        if (_photo.TryGetPhoto(Owner, out var texture, out var name))
        {
            _window.SetImage(texture);
            _window.SetName(name);
            return;
        }

        _window.SetName("Photo");
        _photo.RequestPhoto(Owner);
    }
}
