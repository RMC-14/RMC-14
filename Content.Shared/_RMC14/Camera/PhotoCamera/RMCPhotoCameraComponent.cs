using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Camera.PhotoCamera;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class RMCPhotoCameraComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan? PhotoPrintedAt;

    [DataField, AutoNetworkedField]
    public float ZoomLevel = 0.25f;

    [DataField, AutoNetworkedField]
    public int Resolution = 640;

    [DataField, AutoNetworkedField]
    public TimeSpan PrintDelay = TimeSpan.FromSeconds(2);

    [DataField]
    public byte[]? ImageData;

    [DataField]
    public EntProtoId PhotoPrototype = "RMCCameraPhotoPicture";

    [DataField]
    public SoundSpecifier ShutterSound = new SoundCollectionSpecifier("RMCPolaroid");
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
