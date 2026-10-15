using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Charge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoChargeSystem))]
public sealed partial class XenoChargeWindupComponent : Component
{
    [DataField, AutoNetworkedField]
    public int FrontalArmor = 15;
}
