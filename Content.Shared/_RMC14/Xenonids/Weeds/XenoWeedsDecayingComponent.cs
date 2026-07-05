namespace Content.Shared._RMC14.Xenonids.Weeds;

/// <summary>
/// This is just a copy of <see cref="Robust.Shared.Spawners.TimedDespawnComponent"/> made for xeno weeds,
/// so that they have a chance to cancel the despawn if a new parent node is found.
/// </summary>
[RegisterComponent]
[Access(typeof(SharedXenoWeedsSystem))]
public sealed partial class XenoWeedsDecayingComponent : Component
{
    [DataField]
    public float Lifetime;
}
