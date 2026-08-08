using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Fishing;

[RegisterComponent, NetworkedComponent]
public sealed partial class XenoFishingComponent : Component
{
    [DataField]
    public float FailChance = 0.6f;

    [DataField]
    public int CommonWeight = 60;

    [DataField]
    public int UncommonWeight = 15;

    [DataField]
    public int RareWeight = 5;

    [DataField]
    public int UltraRareWeight = 1;

    [DataField]
    public ProtoId<RMCFishingLootPrototype> Loot = "RMCFishingLootGeneric";
}
