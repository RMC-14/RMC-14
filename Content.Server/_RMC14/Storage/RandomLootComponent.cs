using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Storage;

/// <summary>
/// Generates one configured loot outcome on unpacking, destruction, or an explicit system call.
/// </summary>
[RegisterComponent, Access(typeof(RandomLootSystem))]
public sealed partial class RandomLootComponent : Component
{
    [DataField(required: true)]
    public List<RandomLootOutcome> Outcomes = new();

    [DataField]
    public bool GenerateOnUnpack = true;

    [DataField]
    public bool GenerateOnDestruction;

    [DataField]
    public bool ReplaceSpawnOnTerminate;

    [DataField]
    public LocId? PrimedPopup;

    [DataField]
    public PopupType PrimedPopupType = PopupType.LargeCaution;

    public bool Generated;
}

[DataDefinition]
public sealed partial class RandomLootOutcome
{
    [DataField]
    public float Weight = 1;

    [DataField]
    public EntityTableSelector Contents = new NoneSelector();

    [DataField]
    public ProtoId<RandomHumanoidSettingsPrototype>? Corpse;

    /// <summary>
    /// Localized humanoid name; defaults to the source entity's name.
    /// </summary>
    [DataField]
    public LocId? CorpseName;

    [DataField]
    public LocId? Popup;

    [DataField]
    public PopupType PopupType = PopupType.Small;

    [DataField]
    public float PrimeChance;

    [DataField]
    public float Scatter;

    [DataField]
    public Vector2i? StackAmount;
}
