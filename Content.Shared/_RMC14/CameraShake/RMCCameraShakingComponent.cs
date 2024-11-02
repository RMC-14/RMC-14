using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.CameraShake;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCCameraShakeSystem))]
public sealed partial class RMCCameraShakingComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Spacing = TimeSpan.FromSeconds(0.2);

    [DataField, AutoNetworkedField]
    public TimeSpan NextShake;

    [DataField, AutoNetworkedField]
    public int Shakes;

    [DataField, AutoNetworkedField]
    public int Strength;
}
