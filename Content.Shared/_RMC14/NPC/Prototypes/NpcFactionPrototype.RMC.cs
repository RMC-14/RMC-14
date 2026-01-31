using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.NPC.Prototypes;

public sealed partial class NpcFactionPrototype
{
    [DataField]
    public LocId Name { get; private set; } = string.Empty;

    [DataField]
    public Color Color { get; private set; } = Color.FromHex("#696969");

    [DataField]
    public List<ProtoId<NpcFactionPrototype>>? Subgroups { get; private set; }
}
