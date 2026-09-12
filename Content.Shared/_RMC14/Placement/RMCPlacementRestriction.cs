using Content.Shared.Whitelist;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Placement;

/// <summary>
///     Prevents placement near anchored entities matching <see cref="Blacklist"/>.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class RMCPlacementRestriction
{
    /// <summary>
    ///     Entities matching this whitelist block placement.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Blacklist;

    /// <summary>
    ///     Square radius in tiles. Zero only checks the destination tile.
    /// </summary>
    [DataField]
    public int Radius;
}
