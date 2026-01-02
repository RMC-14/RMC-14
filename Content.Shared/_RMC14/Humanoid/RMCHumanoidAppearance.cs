using Content.Shared.DisplacementMap;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Inventory;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Humanoid;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class RMCHumanoidAppearance : IRMCHumanoidAppearance
{
    public MarkingSet ClientOldMarkings { get; set; } = new();

    [DataField]
    public MarkingSet MarkingSet { get; set; } = new();

    [DataField]
    public Dictionary<HumanoidVisualLayers, HumanoidSpeciesSpriteLayer> BaseLayers { get; set; } = new();

    [DataField]
    public HashSet<HumanoidVisualLayers> PermanentlyHidden { get; set; } = new();

    // Couldn't these be somewhere else?

    [DataField]
    public Gender Gender { get; set; }

    [DataField]
    public int Age { get; set; } = 18;

    /// <summary>
    ///     Any custom base layers this humanoid might have. See:
    ///     limb transplants (potentially), robotic arms, etc.
    ///     Stored on the server, this is merged in the client into
    ///     all layer settings.
    /// </summary>
    [DataField]
    public Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> CustomBaseLayers { get; set; } = new();

    /// <summary>
    ///     Current species. Dictates things like base body sprites,
    ///     base humanoid to spawn, etc.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<SpeciesPrototype> Species { get; set; }

    /// <summary>
    ///     The initial profile and base layers to apply to this humanoid.
    /// </summary>
    [DataField]
    public ProtoId<HumanoidProfilePrototype>? Initial { get; private set; }

    /// <summary>
    ///     Skin color of this humanoid.
    /// </summary>
    [DataField]
    public Color SkinColor { get; set; } = Color.FromHex("#C0967F");

    /// <summary>
    ///     A map of the visual layers currently hidden to the equipment
    ///     slots that are currently hiding them. This will affect the base
    ///     sprite on this humanoid layer, and any markings that sit above it.
    /// </summary>
    [DataField]
    public Dictionary<HumanoidVisualLayers, SlotFlags> HiddenLayers { get; set; } = new();

    [DataField]
    public Sex Sex { get; set; } = Sex.Male;

    [DataField]
    public Color EyeColor { get; set; } = Color.Brown;

    /// <summary>
    ///     Hair color of this humanoid. Used to avoid looping through all markings
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Color? CachedHairColor { get; set; }

    /// <summary>
    ///     Facial Hair color of this humanoid. Used to avoid looping through all markings
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Color? CachedFacialHairColor { get; set; }

    /// <summary>
    ///     Which layers of this humanoid that should be hidden on equipping a corresponding item..
    /// </summary>
    [DataField]
    public HashSet<HumanoidVisualLayers> HideLayersOnEquip { get; set; } = [HumanoidVisualLayers.Hair];

    /// <summary>
    ///     Which markings the humanoid defaults to when nudity is toggled off.
    /// </summary>
    [DataField]
    public ProtoId<MarkingPrototype>? UndergarmentTop { get; set; } = new ProtoId<MarkingPrototype>("UndergarmentTopTanktop");

    [DataField]
    public ProtoId<MarkingPrototype>? UndergarmentBottom { get; set; } = new ProtoId<MarkingPrototype>("UndergarmentBottomBoxers");

    /// <summary>
    ///     The displacement maps that will be applied to specific layers of the humanoid.
    /// </summary>
    [DataField]
    public Dictionary<HumanoidVisualLayers, DisplacementData> MarkingsDisplacement { get; set; } = new();
}
