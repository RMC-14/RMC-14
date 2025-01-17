using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Squads;

/// <summary>
/// Transforms the entity based on the parent's squad. Mainly used by squad-specific items in loadouts.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SquadSystem))]
public sealed partial class RMCMapToSquadComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId, EntProtoId> Map = new();
}
