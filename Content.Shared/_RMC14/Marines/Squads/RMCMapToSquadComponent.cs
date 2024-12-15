using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Squads;

/// <summary>
/// Component which maps other items when it is equipped via starting gear. Mainly used by squad-specific items in loadouts.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SquadSystem))]
public sealed partial class RMCMapToSquadComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId, EntProtoId> Map = new();
}
