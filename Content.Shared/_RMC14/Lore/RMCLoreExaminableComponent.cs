using Robust.Shared.GameStates;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.RMCLoreExaminable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCLoreExaminableComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string Content = string.Empty;

    [DataField, AutoNetworkedField]
    public List<ProtoId<NpcFactionPrototype>>? Factions;
}
