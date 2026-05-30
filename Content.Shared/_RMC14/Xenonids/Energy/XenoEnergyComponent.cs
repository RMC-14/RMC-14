using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Energy;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoEnergySystem))]
public sealed partial class XenoEnergyComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Current;

    [DataField, AutoNetworkedField]
    public int Max = 350;

    [DataField, AutoNetworkedField]
    public int Gain = 5;

    [DataField, AutoNetworkedField]
    public TimeSpan GainEvery = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextGain;

    [DataField, AutoNetworkedField]
    public int GainAttack = 50;

    [DataField, AutoNetworkedField]
    public int GainAttackDowned = 50;

    [DataField, AutoNetworkedField]
    public bool IgnoreLateInfected = false;

    [DataField, AutoNetworkedField]
    public bool GainOnProjectiles = true;

    [DataField, AutoNetworkedField]
    public string PopupGain = "rmc-xeno-energy-increase-user";

    [DataField, AutoNetworkedField]
    public string PopupNotEnough = "rmc-xeno-not-enough-energy";

    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype> Alert = "XenoEnergyBase";

    [DataField, AutoNetworkedField]
    public int? GenerationCap;
}
