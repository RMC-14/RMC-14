using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared._RMC14.Construction.Prototypes;

namespace Content.Shared._RMC14.Construction;

[Serializable, NetSerializable]
public enum RMCConstructionUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RMCConstructionBuiMsg(ProtoId<RMCConstructionPrototype> build, int amount) : BoundUserInterfaceMessage
{
    public readonly ProtoId<RMCConstructionPrototype> Build = build;
    public readonly int Amount = amount;
}

[Serializable, NetSerializable]
public sealed class RMCConstructionBuiState(string stackAmount) : BoundUserInterfaceState
{
    public readonly string StackAmount = stackAmount;
}
