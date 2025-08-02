using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Wheelchair;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(WheelchairSystem))]
public sealed partial class WheelchairComponent : Component
{
    [DataField, AutoNetworkedField]
    public float SpeedMultiplier = 1.0f;
}