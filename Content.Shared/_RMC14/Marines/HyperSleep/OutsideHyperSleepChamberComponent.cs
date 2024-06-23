using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Marines.HyperSleep;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedHyperSleepChamberSystem))]
public sealed partial class OutsideHyperSleepChamberComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Chamber;
}
