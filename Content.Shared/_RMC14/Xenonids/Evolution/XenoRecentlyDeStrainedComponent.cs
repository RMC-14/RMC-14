using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Evolution;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoEvolutionSystem))]
public sealed partial class XenoRecentlyDeStrainedComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan LastDestrain;

    [DataField, AutoNetworkedField]
    public bool HasDestrained = false;
}
