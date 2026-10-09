using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Chemistry.Centrifuge;

[Serializable, NetSerializable]
public enum RMCCentrifugeUi
{
    Key,
}

[Serializable, NetSerializable]
public enum CentrifugeMode : byte
{
    Split,
    Distribute,
}

[Serializable, NetSerializable]
public enum CentrifugeInputSource : byte
{
    Container,
    Turing,
}

[Serializable, NetSerializable]
public sealed class RMCCentrifugeToggleModeBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCCentrifugeToggleSourceBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCCentrifugeSetLabelBuiMsg(string label) : BoundUserInterfaceMessage
{
    public readonly string Label = label;
}

[Serializable, NetSerializable]
public sealed class RMCCentrifugeAttemptConnectionBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCCentrifugeEjectInputBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCCentrifugeEjectOutputBuiMsg : BoundUserInterfaceMessage;
