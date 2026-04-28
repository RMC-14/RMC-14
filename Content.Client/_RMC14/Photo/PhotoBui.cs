using Content.Client._RMC14.Camera;
using Content.Shared._RMC14.Camera.PhotoCamera;
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
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not PhotoBoundUserInterfaceState cast)
            return;

        var photo = _photo.GetPhoto(cast.ImageData);

        _window.SetImage(photo);
        _window.SetName(cast.PhotoName);
    }
}
