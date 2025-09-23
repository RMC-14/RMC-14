using Content.Shared._RMC14.Humanoid;
using Content.Shared.DisplacementMap;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Inventory;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid;

[NetworkedComponent, RegisterComponent, AutoGenerateComponentState(true)]
public sealed partial class HumanoidAppearanceComponent : Component, IRMCHumanoidAppearance
{
    public MarkingSet ClientOldMarkings { get; set; } = new();

    [DataField, AutoNetworkedField]
    public MarkingSet MarkingSet { get; set; } = new();

    [DataField]
    public Dictionary<HumanoidVisualLayers, HumanoidSpeciesSpriteLayer> BaseLayers { get; set; } = new();

    [DataField, AutoNetworkedField]
    public HashSet<HumanoidVisualLayers> PermanentlyHidden { get; set; } = new();

    // Couldn't these be somewhere else?

    [DataField, AutoNetworkedField]
    public Gender Gender { get; set; }

    [DataField, AutoNetworkedField]
    public int Age { get; set; } = 18;

    /// <summary>
    ///     Any custom base layers this humanoid might have. See:
    ///     limb transplants (potentially), robotic arms, etc.
    ///     Stored on the server, this is merged in the client into
    ///     all layer settings.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> CustomBaseLayers { get; set; } = new();

    /// <summary>
    ///     Current species. Dictates things like base body sprites,
    ///     base humanoid to spawn, etc.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<SpeciesPrototype> Species { get; set; }

    /// <summary>
    ///     The initial profile and base layers to apply to this humanoid.
    /// </summary>
    [DataField]
    public ProtoId<HumanoidProfilePrototype>? Initial { get; private set; }

    /// <summary>
    ///     Skin color of this humanoid.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color SkinColor { get; set; } = Color.FromHex("#C0967F");

    /// <summary>
    ///     A map of the visual layers currently hidden to the equipment
    ///     slots that are currently hiding them. This will affect the base
    ///     sprite on this humanoid layer, and any markings that sit above it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<HumanoidVisualLayers, SlotFlags> HiddenLayers { get; set; } = new();

    [DataField, AutoNetworkedField]
    public Sex Sex { get; set; } = Sex.Male;

    [DataField, AutoNetworkedField]
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

[DataDefinition]
[Serializable, NetSerializable]
public readonly partial struct CustomBaseLayerInfo
{
    public CustomBaseLayerInfo(string? id, Color? color = null)
    {
        DebugTools.Assert(id == null || IoCManager.Resolve<IPrototypeManager>().HasIndex<HumanoidSpeciesSpriteLayer>(id));
        Id = id;
        Color = color;
    }

    /// <summary>
    ///     ID of this custom base layer. Must be a <see cref="HumanoidSpeciesSpriteLayer"/>.
    /// </summary>
    [DataField]
    public ProtoId<HumanoidSpeciesSpriteLayer>? Id { get; init; }

    /// <summary>
    ///     Color of this custom base layer. Null implies skin colour if the corresponding <see cref="HumanoidSpeciesSpriteLayer"/> is set to match skin.
    /// </summary>
    [DataField]
    public Color? Color { get; init; }
}
