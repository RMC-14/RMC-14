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
    public readonly int Choice;

    public XenoEvolveBuiMessage(int choice)
    {
        Choice = choice;
    }
}
