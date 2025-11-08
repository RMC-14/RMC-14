using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.ViewVariables;

namespace Content.Shared._RMC14.Xenonids.Weeds;

/// <summary>
/// Blocks weeds from being placed within a certain radius around this entity.
/// Can be configured with a timer or be permanent.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(WeedKillerAuraSystem))]
public sealed partial class WeedKillerAuraComponent : Component
{
    /// <summary>
    /// Radius in tiles around the entity where weeds cannot be placed
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public int BlockRadius = 2;

    /// <summary>
    /// Duration that the weed blocking is active. Zero means lasts forever.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan BlockDuration;

    /// <summary>
    /// Time when the weed blocking will expire. Zero means never expires (calculated on MapInit from BlockDuration).
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan ExpireAt;

    /// <summary>
    /// Whether the blocker is currently active
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Active = true;

    /// <summary>
    /// List of blocker entities spawned around this entity
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> BlockerEntities = new();
}
