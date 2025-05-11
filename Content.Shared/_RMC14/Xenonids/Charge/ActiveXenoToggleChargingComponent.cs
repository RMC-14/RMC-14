using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Charge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoChargeSystem))]
public sealed partial class ActiveXenoToggleChargingComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Distance;

    [DataField, AutoNetworkedField]
    public float SpeedMultiplier = 1;

    [DataField, AutoNetworkedField]
    public MoveButtons Direction;
}
