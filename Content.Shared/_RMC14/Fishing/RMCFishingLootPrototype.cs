using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Fishing;

[Prototype("rmcFishingLoot")]
public sealed partial class RMCFishingLootPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField]
    public List<EntProtoId> Common = new();

    [DataField]
    public List<EntProtoId> Uncommon = new();

    [DataField]
    public List<EntProtoId> Rare = new();

    [DataField]
    public List<EntProtoId> UltraRare = new();
}
