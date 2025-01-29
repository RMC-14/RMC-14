using Robust.Shared.GameStates;

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

    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan LastActivated;
}
