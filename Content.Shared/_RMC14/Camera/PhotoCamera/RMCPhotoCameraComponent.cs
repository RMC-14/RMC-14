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
    ///     The time when the current photo will finish printing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? PhotoPrintedAt;

    /// <summary>
    ///     The zoom level applied when capturing a photo.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ZoomLevel = 0.125f;

    /// <summary>
    ///     The minimum zoom level used as the baseline for all zoom calculations.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BaseZoomLevel = 0.025f;

    /// <summary>
    ///     The incremental zoom change applied per zoom mode step.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ZoomStep = 0.05f;

    /// <summary>
    ///     The resolution of the eye used to take the photo.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Resolution = 1280;

    /// <summary>
    ///     The current zoom preset determining how zoom step modifiers are applied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public PhotoZoomMode ZoomMode = PhotoZoomMode.Standard;

    /// <summary>
    ///     The delay between capturing a photo and the printed photo being created.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan PrintDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    ///     Prototype ID of the action used to cycle camera zoom modes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "RMCActionCycleCameraZoom";

    /// <summary>
    ///     The action id.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Action;

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

[Serializable, NetSerializable]
public sealed class PhotoBoundUserInterfaceState(byte[] imageData, string name) : BoundUserInterfaceState
{
    public byte[] ImageData = imageData;
    public string PhotoName = name;
}
