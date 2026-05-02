using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Camera.PhotoCamera;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class RMCPhotoCameraComponent : Component
{
    /// <summary>
    ///     Remaining number of photos the camera can take before requiring new film.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int RemainingCharges = 10;

    /// <summary>
    ///     Whether the camera can be recharged.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanBeRecharged = true;

    /// <summary>
    ///     The time when the current photo will finish printing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? PhotoPrintedAt;

    /// <summary>
    ///     The zoom level applied when capturing a photo.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ZoomLevel = 0.25f;

    /// <summary>
    ///     The resolution of the eye used to take the photo.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Resolution = 640;

    /// <summary>
    ///     The base resolution of the eye used to take the photo, this is multiplied by the currently selected <see cref="ZoomMode"/> to get the final resolution.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int BaseResolution = 128;

    /// <summary>
    ///     The current zoom preset determining how zoom step modifiers are applied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public PhotoZoomMode ZoomMode = PhotoZoomMode.Standard;

    /// <summary>
    ///     Whether the photo location will be snapped to the center of the clicked tile.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AutoCenter = true;

    /// <summary>
    ///     The delay between capturing a photo and the printed photo being created.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan PrintDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    ///     Prototype ID of the action used to cycle camera zoom modes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId CycleZoomActionId = "RMCActionCycleCameraZoom";

    /// <summary>
    ///     The cycle camera zoom action id.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? CycleZoomAction;

    /// <summary>
    ///     Prototype of the ID of the action used to toggle the camera's autofocus.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId AutoCenterActionId = "RMCActionToggleCameraAutoCenter";

    /// <summary>
    ///     The autofocus action id.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? AutoCenterAction;

    /// <summary>
    ///     The temporary eye used by the camera.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NetEntity? Eye;

    /// <summary>
    ///     The maximum distance at which the camera can capture a photo.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 12f;

    /// <summary>
    ///     Image data that will be stored in the printed photo entity's <see cref="RMCPhotoComponent"/>.
    /// </summary>
    [DataField]
    public byte[]? ImageData;

    /// <summary>
    ///     The identifier of the user that rendered the photo.
    /// </summary>
    [DataField]
    public Guid? ImageRenderedBy;

    /// <summary>
    ///     Prototype ID of the printed photo entity spawned from the camera.
    /// </summary>
    [DataField]
    public EntProtoId PhotoPrototype = "RMCPhotoCameraPicture";

    /// <summary>
    ///     The sound played when a photo is captured.
    /// </summary>
    [DataField]
    public SoundSpecifier ShutterSound = new SoundCollectionSpecifier("RMCPolaroid");

    /// <summary>
    ///     The sound played when cycling between zoom modes.
    /// </summary>
    [DataField]
    public SoundSpecifier? CycleZoomSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/safety_toggle.ogg", AudioParams.Default.WithMaxDistance(6).WithVariation(0.125f));

    /// <summary>
    ///     The sound played when a new roll of film is inserted into the camera.
    /// </summary>
    [DataField]
    public SoundSpecifier? FilmInsertSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/revolver_magin.ogg");
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
