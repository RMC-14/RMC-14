using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.ManageHive;

[Serializable, NetSerializable]
public record ManageHiveDevolveConfirmEvent(EntProtoId Choice);
