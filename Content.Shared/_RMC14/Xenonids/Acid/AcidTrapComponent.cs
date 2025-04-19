using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Acid;

/// <summary>
/// Used on traps for comparing acid levels and setting stuff up
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AcidTrapComponent : Component
{
    [DataField, AutoNetworkedField]
    public int TrapLevel = 2;

    [DataField, AutoNetworkedField]
    public int Cost = 100;

    [DataField, AutoNetworkedField]
    public EntProtoId Spray = "XenoAcidSprayTrap";
}
