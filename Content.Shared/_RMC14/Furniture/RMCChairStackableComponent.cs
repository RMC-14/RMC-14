using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Furniture;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class RMCChairStackableComponent : Component
{
    /// <summary>
    /// Maximum number of stacked chairs that is stable.
    /// Beyond this, the stack has a chance to collapse when adding more.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxStableStack = 8;

    /// <summary>
    /// Current number of extra folded chairs stacked on this chair.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int CurrentStackSize;

    /// <summary>
    /// The fixture ID to toggle hard/not-hard when stacking/unstacking.
    /// When stacked, this fixture becomes hard. :godo:
    /// </summary>
    [DataField]
    public string StackFixtureId = "stack_block";

    [DataField]
    public float StackFixtureRadius = 0.35f;

    [DataField]
    public TimeSpan ThrownMobStatusDuration = TimeSpan.FromSeconds(4);

    [DataField]
    public SoundSpecifier? CollapseSound = new SoundPathSpecifier("/Audio/_RMC14/Items/metal_chair_crash.ogg");

    [DataField]
    public SoundSpecifier? ThrownHitSound = new SoundPathSpecifier("/Audio/_RMC14/Items/metal_chair_slam.ogg");
}

[Serializable, NetSerializable]
public enum RMCChairStackVisuals : byte
{
    StackSize
}
