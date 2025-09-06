using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Chemistry;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedRMCChemistrySystem))]
public sealed partial class RMCChemicalStorageComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Energy = 50;

    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxEnergy;

    [DataField, AutoNetworkedField]
    public FixedPoint2 BaseMax = 100;

    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxPer = 100;

    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Network;

    [DataField, AutoNetworkedField]
    public TimeSpan RechargeEvery = TimeSpan.FromSeconds(52.5);

    [DataField, AutoNetworkedField]
    public FixedPoint2 Recharge;

    [DataField, AutoNetworkedField]
    public FixedPoint2 BaseRecharge = 10;

    [DataField, AutoNetworkedField]
    public FixedPoint2 RechargePer = 5;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan RechargeAt;

    [DataField, AutoNetworkedField]
    public bool Updated;
}
