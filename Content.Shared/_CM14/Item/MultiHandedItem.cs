using Content.Shared.Whitelist;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Item;

[DataRecord]
[Serializable, NetSerializable]
public record MultiHandedItem(int Hands, EntityWhitelist Whitelist);
