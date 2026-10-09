using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Actions;

/// <summary>
///     Actions with this component should remain usable even when the entity is in a container that normally disables actions
/// </summary>
[RegisterComponent]
[Access(typeof(SharedRMCActionsSystem))]
public sealed partial class RMCInContainerActionComponent : Component
{
}
