using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Water;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(RMCWaterSystem))]
public sealed partial class ToxicWaterComponent : Component
{
    [DataField, AutoNetworkedField]
    public float VehicleDamage = 10;

    [DataField, AutoNetworkedField]
    public TimeSpan VehicleDamageEvery = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextVehicleDamageAt;
}
