using Content.Shared._RMC14.OrbitalCannon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Intel.Tech;

[DataRecord]
[Serializable, NetSerializable]
public sealed partial record TechLogisticsDeliveryEvent(EntProtoId Object);
