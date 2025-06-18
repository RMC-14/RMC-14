using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Kitchen
{
    public sealed class SharedProcessor
    {
        public static string BeakerSlotId = "beakerSlot";

        public static string InputContainerId = "inputContainer";
    }

    [Serializable, NetSerializable]
    public sealed class ProcessorStartMessage : BoundUserInterfaceMessage
    {
        public readonly ProcessorProgram Program;
        public ProcessorStartMessage(ProcessorProgram program)
        {
            Program = program;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ProcessorEjectChamberAllMessage : BoundUserInterfaceMessage
    {
        public ProcessorEjectChamberAllMessage()
        {
        }
    }

    [Serializable, NetSerializable]
    public sealed class ProcessorEjectChamberContentMessage : BoundUserInterfaceMessage
    {
        public NetEntity EntityId;
        public ProcessorEjectChamberContentMessage(NetEntity entityId)
        {
            EntityId = entityId;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ProcessorWorkStartedMessage : BoundUserInterfaceMessage
    {
        public ProcessorProgram ProcessorProgram;
        public ProcessorWorkStartedMessage(ProcessorProgram processorProgram)
        {
            ProcessorProgram = processorProgram;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ProcessorWorkCompleteMessage : BoundUserInterfaceMessage
    {
        public ProcessorWorkCompleteMessage()
        {
        }
    }

    [Serializable, NetSerializable]
    public enum ProcessorVisualState : byte
    {
        On
    }

    [Serializable, NetSerializable]
    public enum ProcessorProgram : byte
    {
        Process
    }

    [NetSerializable, Serializable]
    public enum ProcessorUiKey : byte
    {
        Key
    }

    [NetSerializable, Serializable]
    public sealed class ProcessorInterfaceState : BoundUserInterfaceState
    {
        public bool IsBusy;
        public bool IsOn;
        public bool Powered;
        public bool CanProcess;
        public NetEntity[] ChamberContents;
        public ReagentQuantity[]? ReagentQuantities;

        public ProcessorInterfaceState(bool isBusy, bool hasBeaker, bool powered, bool canProcess, NetEntity[] chamberContents, ReagentQuantity[]? heldBeakerContents)
        {
            IsBusy = isBusy;
            Powered = powered;
            CanProcess = canProcess;
            ChamberContents = chamberContents;
            ReagentQuantities = heldBeakerContents;
        }
    }
}
