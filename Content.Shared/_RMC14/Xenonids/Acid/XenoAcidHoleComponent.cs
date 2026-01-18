using Robust.Shared.Maths;

namespace Content.Shared._RMC14.Xenonids.Acid;

[RegisterComponent]
[Access(typeof(XenoAcidHoleSystem))]
public sealed partial class XenoAcidHoleComponent : Component
{
    [DataField]
    public TimeSpan CrawlDelay = TimeSpan.FromSeconds(1.5);

    [DataField]
    public TimeSpan RepairDelay = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan BreakDelay = TimeSpan.FromSeconds(10);

    [DataField]
    public int RepairMaterialCost = 3;

    [DataField]
    public int RepairNailCost = 4;

    [DataField]
    public int BigXenoDamageMin = 2000;

    [DataField]
    public int BigXenoDamageMax = 3500;

    public EntityUid? Wall;
    public Direction EntranceDirection = Direction.South;
}
