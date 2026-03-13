using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.SecureSafe;

[Serializable, NetSerializable]
public enum RMCSafeUIKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class RMCSafeBuiState : BoundUserInterfaceState
{
    public readonly int Dial1;
    public readonly int Dial2;

    public RMCSafeBuiState(int dial1, int dial2)
    {
        Dial1 = dial1;
        Dial2 = dial2;
    }
}

[Serializable, NetSerializable]
public sealed class RMCSafeChangeDialMessage : BoundUserInterfaceMessage
{
    public readonly int DialNumber;
    public readonly int Amount;

    public RMCSafeChangeDialMessage(int dialNumber, int amount)
    {
        DialNumber = dialNumber;
        Amount = amount;
    }
}

[Serializable, NetSerializable]
public sealed class RMCSafeTryOpenMessage : BoundUserInterfaceMessage
{
}
