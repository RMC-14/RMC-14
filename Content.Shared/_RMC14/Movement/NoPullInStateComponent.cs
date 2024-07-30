using Content.Shared.Mobs;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Movement;

/// <summary>
/// Prevents this mob being pulled when in a certain state.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(NoPullInStateSystem))]
public sealed partial class NoPullInStateComponent : Component
{
    /// <summary>
    /// The state it cannot be pulled in.
    /// </summary>
    [DataField(required: true)]
    public MobState State = MobState.Invalid;
}
