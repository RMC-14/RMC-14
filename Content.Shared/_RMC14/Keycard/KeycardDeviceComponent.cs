using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Keycard;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(KeycardDeviceSystem))]
public sealed partial class KeycardDeviceComponent : Component
{
    [DataField, AutoNetworkedField]
    public KeycardDeviceMode Mode = KeycardDeviceMode.None;

    [DataField, AutoNetworkedField]
    public float Range = 10;

    [DataField, AutoNetworkedField]
    public TimeSpan Time = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public bool Active;

    [DataField]
    public EntityUid? RequestSource;

    [DataField]
    public EntityUid? Initiator;

    [DataField, AutoPausedField]
    public TimeSpan RequestExpiresAt;
}

[Serializable, NetSerializable]
public enum KeycardDeviceVisuals
{
    Active,
}
