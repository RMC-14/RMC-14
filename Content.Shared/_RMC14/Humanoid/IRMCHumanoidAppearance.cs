using Content.Shared.DisplacementMap;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Inventory;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Humanoid;

public interface IRMCHumanoidAppearance
{
    public MarkingSet ClientOldMarkings { get; set; }
    public MarkingSet MarkingSet { get; set; }
    public Dictionary<HumanoidVisualLayers, HumanoidSpeciesSpriteLayer> BaseLayers { get; set; }
    public HashSet<HumanoidVisualLayers> PermanentlyHidden { get; set; }
    public Gender Gender { get; set; }
    public int Age { get; set; }
    public Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> CustomBaseLayers { get; set; }
    public ProtoId<SpeciesPrototype> Species { get; set; }
    public ProtoId<HumanoidProfilePrototype>? Initial { get; }
    public Color SkinColor { get; set; }
    public Dictionary<HumanoidVisualLayers, SlotFlags> HiddenLayers { get; set; }
    public Sex Sex { get; set; }
    public Color EyeColor { get; set; }
    public Color? CachedHairColor { get; set; }
    public Color? CachedFacialHairColor { get; set; }
    public HashSet<HumanoidVisualLayers> HideLayersOnEquip { get; set; }
    public ProtoId<MarkingPrototype>? UndergarmentTop { get; set; }
    public ProtoId<MarkingPrototype>? UndergarmentBottom { get; set; }
    public Dictionary<HumanoidVisualLayers, DisplacementData> MarkingsDisplacement { get; set; }
}
