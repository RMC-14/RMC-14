using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Scoping;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedScopeSystem))]
public sealed partial class ScopeComponent : Component
{
    /// <summary>
    /// The entity that's scoping
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? User;

    /// <summary>
    /// Value to which zoom will be set when scoped in
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Zoom = 1f;

    [DataField, AutoNetworkedField]
    public EntProtoId ScopingToggleAction = "CMActionToggleScope";

    [DataField, AutoNetworkedField]
    public EntityUid? ScopingToggleActionEntity;

    [DataField, AutoNetworkedField]
    public bool RequireWielding;

    [DataField, AutoNetworkedField]
    public Direction? ScopingDirection;

    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public EntityUid? RelayEntity;
}
