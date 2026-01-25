using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedRMCFlammableSystem))]
public sealed partial class SteppingOnFireComponent : Component
{
    [DataField, AutoNetworkedField]
    public double ArmorMultiplier = 1;

    [DataField, AutoNetworkedField]
    public float Distance;

    [DataField, AutoNetworkedField]
    public TimeSpan UpdateTime = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan UpdateAt = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public EntityCoordinates? LastPosition;
}
