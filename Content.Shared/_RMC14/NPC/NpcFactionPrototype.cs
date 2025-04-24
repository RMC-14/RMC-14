using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Shared.NPC.Prototypes;

public sealed partial class NpcFactionPrototype : IPrototype
{
    [DataField]
    public HashSet<ProtoId<RadioChannelPrototype>> Channels = new();
}
