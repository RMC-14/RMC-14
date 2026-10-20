using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.NPC.Prototypes;

public sealed partial class NpcFactionPrototype
{
    [DataField]
    public LocId? Name;

    [DataField]
    public Color Color = Color.FromHex("#696969");

    [DataField]
    public List<ProtoId<NpcFactionPrototype>>? Subgroups;
}
