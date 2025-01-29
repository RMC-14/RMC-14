using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Construction;

[Serializable, NetSerializable]
public enum RMCConstructionUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RMCConstructionBuiMsg(ProtoId<JobPrototype> build, int amount) : BoundUserInterfaceMessage
{
    public readonly ProtoId<JobPrototype> Build = build;
    public readonly int Amount = amount;
}
