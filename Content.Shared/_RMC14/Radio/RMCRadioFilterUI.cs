using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.Radio;

namespace Content.Shared._RMC14.Radio;

[Serializable, NetSerializable]
public enum RMCRadioFilterUI
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RMCRadioFilterBuiMsg(ProtoId<RadioChannelPrototype> channel, bool toggle) : BoundUserInterfaceMessage
{
    public readonly ProtoId<RadioChannelPrototype> Channel = channel;
    public readonly bool Toggle = toggle;
}
