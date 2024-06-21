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

    // TODO CM14 scoping making this too high causes pop-in
    // wait until https://github.com/space-wizards/RobustToolbox/pull/5228 is fixed to increase it
    // cm13 values: 11 tile offset, 24x24 view in 4x | 6 tile offset, normal view in 2x.
    // right now we are doing a mix of both and only one setting.
    /// <summary>
    ///     How much to offset the user's view by when scoping.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Offset = 15;

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

    [DataField, AutoNetworkedField]
    public bool Attachment;
}
