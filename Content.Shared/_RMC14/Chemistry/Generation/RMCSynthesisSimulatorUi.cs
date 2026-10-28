using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Chemistry.Generation;

[Serializable, NetSerializable]
public enum RMCSynthesisSimulatorUi : byte
{
    Key,
}

[Serializable, NetSerializable]
public enum RMCSynthesisSimulatorVisuals : byte
{
    State,
}

[Serializable, NetSerializable]
public enum RMCSynthesisSimulatorVisualState : byte
{
    Idle,
    Running,
    Ready,
    Reading,
    Printing,
}

[Serializable, NetSerializable]
public enum SynthesisMode : byte
{
    Amplify,
    Suppress,
    Relate,
    Add,
}

[Serializable, NetSerializable]
public sealed class RMCSynthesisSimulatorSetModeBuiMsg : BoundUserInterfaceMessage
{
    public SynthesisMode Mode;

    public RMCSynthesisSimulatorSetModeBuiMsg(SynthesisMode mode)
    {
        Mode = mode;
    }
}

[Serializable, NetSerializable]
public sealed class RMCSynthesisSimulatorSelectTargetPropertyBuiMsg : BoundUserInterfaceMessage
{
    public ProtoId<ChemGeneratorPropertyPrototype>? PropertyId;

    public RMCSynthesisSimulatorSelectTargetPropertyBuiMsg(ProtoId<ChemGeneratorPropertyPrototype>? propertyId)
    {
        PropertyId = propertyId;
    }
}

[Serializable, NetSerializable]
public sealed class RMCSynthesisSimulatorSelectReferencePropertyBuiMsg : BoundUserInterfaceMessage
{
    public ProtoId<ChemGeneratorPropertyPrototype>? PropertyId;

    public RMCSynthesisSimulatorSelectReferencePropertyBuiMsg(ProtoId<ChemGeneratorPropertyPrototype>? propertyId)
    {
        PropertyId = propertyId;
    }
}

[Serializable, NetSerializable]
public sealed class RMCSynthesisSimulatorPickRecipeBuiMsg : BoundUserInterfaceMessage
{
    public int Index;

    public RMCSynthesisSimulatorPickRecipeBuiMsg(int index)
    {
        Index = index;
    }
}

[Serializable, NetSerializable]
public sealed class RMCSynthesisSimulatorSimulateBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCSynthesisSimulatorCancelSimulationBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCSynthesisSimulatorEjectTargetBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCSynthesisSimulatorEjectReferenceBuiMsg : BoundUserInterfaceMessage;
