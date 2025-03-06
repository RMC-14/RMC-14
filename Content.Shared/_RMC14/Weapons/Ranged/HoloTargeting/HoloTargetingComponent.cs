namespace Content.Shared._RMC14.Weapons.Ranged.HoloTargeting;

[RegisterComponent]
[Access(typeof(RMCHoloTargetingSystem))]
public sealed partial class HoloTargetingComponent : Component
{
    /// <summary>
    ///     The amount of holo stacks the projectile will add
    /// </summary>
    [DataField]
    public float Stacks = 10;

    /// <summary>
    ///     The maximum amount of holo stacks on the target, the projectiles won't apply any stacks if the target has
    ///     this amount of stacks or more.
    /// </summary>
    [DataField]
    public float MaxStacks = 100f;

    /// <summary>
    ///     The decay rate of the applied holo stacks
    /// </summary>
    [DataField]
    public float Decay = 5f;
}
