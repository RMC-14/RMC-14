using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Kitchen.Components
{
    [Serializable, NetSerializable]
    public sealed class ProcessorStartCookMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class ProcessorEjectMessage : BoundUserInterfaceMessage
    {

    }

    [Serializable, NetSerializable]
    public sealed class ProcessorEjectSolidIndexedMessage : BoundUserInterfaceMessage
    {
        public NetEntity EntityID;
        public ProcessorEjectSolidIndexedMessage(NetEntity entityId)
        {
            EntityID = entityId;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ProcessorVaporizeReagentIndexedMessage : BoundUserInterfaceMessage
    {
        public ReagentQuantity ReagentQuantity;
        public ProcessorVaporizeReagentIndexedMessage(ReagentQuantity reagentQuantity)
        {
            ReagentQuantity = reagentQuantity;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ProcessorSelectCookTimeMessage : BoundUserInterfaceMessage
    {
        public int ButtonIndex;
        public uint NewCookTime;
        public ProcessorSelectCookTimeMessage(int buttonIndex, uint inputTime)
        {
            ButtonIndex = buttonIndex;
            NewCookTime = inputTime;
        }
    }

    [NetSerializable, Serializable]
    public sealed class ProcessorUpdateUserInterfaceState : BoundUserInterfaceState
    {
        public NetEntity[] ContainedSolids;
        public bool IsProcessorBusy;
        public int ActiveButtonIndex;
        public uint CurrentCookTime;

        public TimeSpan CurrentCookTimeEnd;

        public ProcessorUpdateUserInterfaceState(NetEntity[] containedSolids,
            bool isProcessorBusy, int activeButtonIndex, uint currentCookTime, TimeSpan currentCookTimeEnd)
        {
            ContainedSolids = containedSolids;
            IsProcessorBusy = isProcessorBusy;
            ActiveButtonIndex = activeButtonIndex;
            CurrentCookTime = currentCookTime;
            CurrentCookTimeEnd = currentCookTimeEnd;
        }

    }

    [Serializable, NetSerializable]
    public enum ProcessorVisualState
    {
        On,
        Processing
    }

    [Serializable, NetSerializable]
    public enum ProcessorProgram : byte
    {
        Process
    }

    [NetSerializable, Serializable]
    public enum ProcessorUiKey
    {
        Key
    }

}
