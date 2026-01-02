using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Actions;

/// <summary>
///     Actions with this component are disabled when the entity is dazed
/// </summary>
[RegisterComponent]
[Access(typeof(SharedRMCActionsSystem))]
public sealed partial class RMCDazeableActionComponent : Component
{
    [DataField]
    public float DurationMultiplier = 1;
}
