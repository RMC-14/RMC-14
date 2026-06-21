using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Intel;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(IntelSystem))]
public sealed partial class IntelDataDiskComponent : Component
{
    [DataField, AutoNetworkedField]
    public string EncryptionKey = string.Empty;

    [DataField, AutoNetworkedField]
    public float UploadProgress;

    [DataField, AutoNetworkedField]
    public float UploadTime = 10;

    [DataField, AutoNetworkedField]
    public FixedPoint2 UploadValue = FixedPoint2.New(0.35);

    [DataField, AutoNetworkedField]
    public bool Uploading;

    [DataField, AutoNetworkedField]
    public bool Completed;

    [DataField, AutoNetworkedField]
    public EntityUid? LastUser;
}
