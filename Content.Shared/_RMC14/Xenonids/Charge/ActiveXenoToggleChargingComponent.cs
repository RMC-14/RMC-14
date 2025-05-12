using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Charge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoChargeSystem))]
public sealed partial class ActiveXenoToggleChargingComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Distance;

    [DataField, AutoNetworkedField]
    public float Steps;

    [DataField, AutoNetworkedField]
    public int Stage;

    [DataField, AutoNetworkedField]
    public MoveButtons Direction;

    [DataField, AutoNetworkedField]
    public float SoundSteps;

    [DataField, AutoNetworkedField]
    public MoveButtons Deviated;

    [DataField, AutoNetworkedField]
    public float DeviatedDistance;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastMovedAt;
}
