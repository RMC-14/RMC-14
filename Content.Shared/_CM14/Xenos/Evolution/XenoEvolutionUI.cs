using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Xenos.Evolution;

[Serializable, NetSerializable]
public enum XenoEvolutionUIKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class XenoEvolveBuiMessage : BoundUserInterfaceMessage
{
    public readonly EntProtoId Choice;

    public XenoEvolveBuiMessage(EntProtoId choice)
    {
        Choice = choice;
    }
}
