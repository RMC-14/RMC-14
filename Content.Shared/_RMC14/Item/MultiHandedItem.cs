using Content.Shared.Whitelist;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Item;

[DataRecord]
[Serializable, NetSerializable]
public record MultiHandedItem(int Hands, EntityWhitelist Whitelist);
