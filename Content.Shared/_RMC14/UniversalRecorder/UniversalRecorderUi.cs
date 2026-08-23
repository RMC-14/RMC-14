using Content.Shared.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.UniversalRecorder;

[Serializable, NetSerializable]
public enum UniversalRecorderUiKey : byte
{
    Recorder,
    Tape,
}

[Serializable, NetSerializable]
public enum UniversalRecorderRecorderAction : byte
{
    Record,
    Play,
    Stop,
    PrintTranscript,
    Eject,
}

[Serializable, NetSerializable]
public enum UniversalRecorderTapeAction : byte
{
    Flip,
    Unwind,
}

[Serializable, NetSerializable]
public sealed class UniversalRecorderRecorderBuiState(UniversalRecorderRecorderAction[] actions) : BoundUserInterfaceState
{
    public readonly UniversalRecorderRecorderAction[] Actions = actions;
}

[Serializable, NetSerializable]
public sealed class UniversalRecorderTapeBuiState(UniversalRecorderTapeAction[] actions) : BoundUserInterfaceState
{
    public readonly UniversalRecorderTapeAction[] Actions = actions;
}

[Serializable, NetSerializable]
public sealed class UniversalRecorderRecorderActionBuiMsg(UniversalRecorderRecorderAction action) : BoundUserInterfaceMessage
{
    public readonly UniversalRecorderRecorderAction Action = action;
}

[Serializable, NetSerializable]
public sealed class UniversalRecorderTapeActionBuiMsg(UniversalRecorderTapeAction action) : BoundUserInterfaceMessage
{
    public readonly UniversalRecorderTapeAction Action = action;
}
