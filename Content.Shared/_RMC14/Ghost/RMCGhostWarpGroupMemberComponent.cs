using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Ghost;

/// <summary>
/// Компонент для указания принадлежности сущности к группе варпов.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCGhostWarpGroupMemberComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<RMCGhostWarpGroupPrototype> Group;
}
