using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged.HoloTargeting;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCHoloTargetedSystem))]
public sealed partial class HoloTargetedComponent : Component
{
    /// <summary>
    ///     The amount of holo stacks the entity currently has, 100 stacks is a 10% increase to any damage received.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Stacks;

    /// <summary>
    ///     The amount of stacks being removed every second.
    /// </summary>
    [DataField]
    public float Decay = 5f;

    /// <summary>
    ///     Ensures the decay amount if being removed every second.
    /// </summary>
    [DataField]
    public float DecayTimer;

    /// <summary>
    ///     The amount of time in seconds of not being hit before stacks start decaying.
    /// </summary>
    [DataField]
    public float DecayDelay = 5;
}
