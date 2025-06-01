using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines;

/// <summary>
///     This component allows you to see marine icons above mobs.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedMarineSystem))]
public sealed partial class ShowMarineIconsComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<NpcFactionPrototype>>? Factions;

    [DataField, AutoNetworkedField]
    public bool BypassFactionIcons = false;
}
