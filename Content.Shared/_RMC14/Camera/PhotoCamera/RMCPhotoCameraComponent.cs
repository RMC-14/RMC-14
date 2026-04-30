using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Camera.PhotoCamera;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class RMCPhotoCameraComponent : Component
{
    /// <summary>
    ///     The time at which the photo will be printed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? PhotoPrintedAt;

    /// <summary>
    ///     The zoom level of the eye used to make the photo.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ZoomLevel = 0.125f;

    /// <summary>
    ///     The base zoom level, used in combination with <see cref="ZoomStep"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BaseZoomLevel = 0.025f;

    /// <summary>
    ///     How much the zoom should be increased per increase in zoom mode.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ZoomStep = 0.05f;

    /// <summary>
    ///     The resolution of the eye used to take the photo.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Resolution = 1280;

    /// <summary>
    ///     The current zoom mode of the camera, determines how many times the <see cref="Zoomstep"/> is multiplied, before being added to the <see cref="BaseZoomLevel"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public PhotoZoomMode ZoomMode = PhotoZoomMode.Standard;

    /// <summary>
    ///     The time between taking the picture and the picture being spawned.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan PrintDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    ///     The action prototype belonging to this action.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "RMCActionCycleCameraZoom";

    /// <summary>
    ///     The action id.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    /// <summary>
    ///     The photo data, only stored temporarily so it can be transferred to the <see cref="RMCPhotoComponent"/> of the printed photo.
    /// </summary>
    [DataField]
    public byte[]? ImageData;

    /// <summary>
    ///     The user that rendered the photo.
    /// </summary>
    [DataField]
    public Guid? ImageRenderedBy;

    /// <summary>
    ///     The photo prototype.
    /// </summary>
    [DataField]
    public EntProtoId PhotoPrototype = "RMCPhotoCameraPicture";

    /// <summary>
    ///     The sound played when a photo is made.
    /// </summary>
    [DataField]
    public SoundSpecifier ShutterSound = new SoundCollectionSpecifier("RMCPolaroid");

    /// <summary>
    ///     The sound played when switching zoom mode.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? CycleZoomSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/safety_toggle.ogg", AudioParams.Default.WithMaxDistance(6).WithVariation(0.125f));
}

[Serializable, NetSerializable]
public enum PhotoZoomMode
{
    Focused,
    Close,
    Standard,
    Wide,
}

[Serializable, NetSerializable]
public enum RMCPhotoUi
{
    Key,
}

[Serializable, NetSerializable]
public sealed class PhotoBoundUserInterfaceState(byte[] imageData, string name) : BoundUserInterfaceState
{
    public byte[] ImageData = imageData;
    public string PhotoName = name;
}
