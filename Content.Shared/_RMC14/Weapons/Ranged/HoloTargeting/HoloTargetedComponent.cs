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
}
