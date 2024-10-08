using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Scoping;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedScopeSystem))]
public sealed partial class ScopeComponent : Component
{
    [DataField, AutoNetworkedField]
    public int CurrentZoomLevel = 0;

    [DataField, AutoNetworkedField]
    public List<ScopeZoomLevel> ZoomLevels = new()
    {
        new ScopeZoomLevel(null, 1f, 15, false, TimeSpan.FromSeconds(1))
    };

    /// <summary>
    /// The entity that's scoping
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? User;

    [DataField, AutoNetworkedField]
    public EntProtoId ScopingToggleAction = "CMActionToggleScope";

    [DataField, AutoNetworkedField]
    public EntityUid? ScopingToggleActionEntity;

    [DataField, AutoNetworkedField]
    public EntProtoId CycleZoomLevelAction = "RMCActionCycleZoomLevel";

    [DataField, AutoNetworkedField]
    public EntityUid? CycleZoomLevelActionEntity;

    [DataField, AutoNetworkedField]
    public bool RequireWielding;

    [DataField, AutoNetworkedField]
    public bool UseInHand;

    [DataField, AutoNetworkedField]
    public Direction? ScopingDirection;

    [DataField, AutoNetworkedField]
    public EntityUid? RelayEntity;

    [DataField, AutoNetworkedField]
    public bool Attachment;
}

[DataRecord, Serializable, NetSerializable]
public record struct ScopeZoomLevel(
    /// <summary>
    /// This is used in the popup when cycling through zoom levels.
    /// </summary>
    string? Name,

    /// <summary>
    /// Value to which zoom will be set when scoped in.
    /// </summary>
    float Zoom,

    // TODO RMC14 scoping making this too high causes pop-in
    // wait until https://github.com/space-wizards/RobustToolbox/pull/5228 is fixed to increase it
    // cm13 values: 11 tile offset, 24x24 view in 4x | 6 tile offset, normal view in 2x.
    // right now we are doing a mix of both and only one setting.
    /// <summary>
    ///     How much to offset the user's view by when scoping.
    /// </summary>
    float Offset,

    /// <summary>
    /// If set to true, the user's movement won't interrupt the scoping action.
    /// </summary>
    bool AllowMovement,

    /// <summary>
    /// The length of the doafter to zoom in.
    /// </summary>
    TimeSpan DoAfter
);
