using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Xenos.Evolution;

[Serializable, NetSerializable]
public enum XenoEvolutionUIKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class EvolveBuiMessage : BoundUserInterfaceMessage
{
    public readonly int Choice;

    public EvolveBuiMessage(int choice)
    {
        Choice = choice;
    }
}
