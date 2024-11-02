using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dialog;

[Serializable, NetSerializable]
public enum DialogUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class DialogBuiState(string title, List<string> options) : BoundUserInterfaceState
{
    public readonly string Title = title;
    public readonly List<string> Options = options;
}

[Serializable, NetSerializable]
public sealed class DialogChosenBuiMsg(int index) : BoundUserInterfaceMessage
{
    public readonly int Index = index;
}
