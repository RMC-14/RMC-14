using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Requisitions.Components;

[DataRecord]
[Serializable, NetSerializable]
public sealed class RequisitionsRandomCrates
{
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Every;

    [ViewVariables(VVAccess.ReadWrite)]
    public int Minimum;

    [ViewVariables(VVAccess.ReadWrite)]
    public int MinimumFor;

    [ViewVariables(VVAccess.ReadWrite)]
    public List<EntProtoId> Choices = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Next;

    [ViewVariables(VVAccess.ReadWrite)]
    public int Given;

    [ViewVariables(VVAccess.ReadWrite)]
    public double Fraction;
}
