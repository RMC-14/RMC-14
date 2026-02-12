using Content.Shared.Actions;
using Content.Shared._RMC14.Xenonids.Eye; // RMC14
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Actions.Components;

/// <summary>
/// An action that raises an event as soon as it gets used.
/// Requires <see cref="ActionComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedActionsSystem), typeof(QueenEyeSystem))] // RMC14
[EntityCategory("Actions")]
public sealed partial class InstantActionComponent : Component
{
    /// <summary>
    ///     The local-event to raise when this action is performed.
    /// </summary>
    [DataField(required: true), NonSerialized]
    public InstantActionEvent? Event;
}
