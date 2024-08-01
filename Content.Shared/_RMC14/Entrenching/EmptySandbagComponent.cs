using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Entrenching;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(BarricadeSystem))]
public sealed partial class EmptySandbagComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId Filled = "CMSandbagFull";
}
