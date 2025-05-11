using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Charge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoChargeSystem))]
public sealed partial class XenoToggleChargingComponent : Component
{
    [DataField, AutoNetworkedField]
    public float MinimumSteps = 4;

    [DataField, AutoNetworkedField]
    public float MaxSpeed = 8;

    [DataField, AutoNetworkedField]
    public float SpeedPerStep = 0.2f;

    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaPerStep = 3;

    [DataField, AutoNetworkedField]
    public FixedPoint2 Shield = 100;

    [DataField, AutoNetworkedField]
    public TimeSpan ShieldDuration = TimeSpan.FromSeconds(4);
}
