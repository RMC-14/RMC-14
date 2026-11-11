using Content.Shared.Eui;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Marines.Icons;

[Serializable, NetSerializable]
public sealed class JobIconPickerSelectMessage : EuiMessageBase
{
    public readonly ResPath Rsi;
    public readonly string State;

    public JobIconPickerSelectMessage(ResPath rsi, string state)
    {
        Rsi = rsi;
        State = state;
    }
}

[Serializable, NetSerializable]
public sealed class JobIconPickerClearMessage : EuiMessageBase
{
}
