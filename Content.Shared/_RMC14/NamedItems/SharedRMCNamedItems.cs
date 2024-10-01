using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.NamedItems;

[DataRecord]
[Serializable, NetSerializable]
public record SharedRMCNamedItems(
    string? PrimaryGunName = null,
    string? SidearmName = null,
    string? HelmetName = null,
    string? ArmorName = null,
    string? SentryName = null
);
