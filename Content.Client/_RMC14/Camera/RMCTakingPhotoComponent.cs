using Content.Shared._RMC14.Camera.PhotoCamera;
using Robust.Shared.Map;

namespace Content.Client._RMC14.Camera;

[RegisterComponent]
public sealed partial class RMCTakingPhotoComponent : Component
{
    [DataField]
    public EntityCoordinates? PhotoCoordinates;

    [DataField]
    public PhotoZoomMode ZoomMode;
}
