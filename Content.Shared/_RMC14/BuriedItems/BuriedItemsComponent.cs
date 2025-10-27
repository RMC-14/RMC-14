using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.BuriedItems;

/// <summary>
/// Marks an entity as containing buried items that can be dug up with an entrenching tool.
/// Uses storage-based inventory that gets pre-filled by loot tables on spawn.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(BuriedItemsSystem), Other = AccessPermissions.ReadWrite)]
public sealed partial class BuriedItemsComponent : Component
{
    /// <summary>
    /// Multiple loot tables - one item will be picked from each table and spawned into storage on MapInit.
    /// Each table can have items with different probabilities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<BuriedItemsLootTable>? LootTables;

    /// <summary>
    /// How long it takes to dig up the buried items.
    /// </summary>
    [DataField]
    public TimeSpan DigDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Sound to play when digging starts.
    /// </summary>
    [DataField]
    public SoundSpecifier DigSound = new SoundCollectionSpecifier("CMEntrenchingThud", AudioParams.Default.WithVolume(-3));

    /// <summary>
    /// Sound to play when items are revealed.
    /// </summary>
    [DataField]
    public SoundSpecifier RevealSound = new SoundPathSpecifier("/Audio/Effects/rustle1.ogg");
}

/// <summary>
/// Represents a single loot table for buried items.
/// One item will be randomly picked from the entries in this table.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public partial struct BuriedItemsLootTable
{
    /// <summary>
    /// List of possible items in this loot table with their spawn chances.
    /// </summary>
    [DataField(required: true)]
    public List<BuriedItemsLootEntry> Entries = new();
}

/// <summary>
/// A single entry in a loot table with its prototype and weight.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public partial struct BuriedItemsLootEntry
{
    /// <summary>
    /// The entity prototype to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Id;

    /// <summary>
    /// The relative weight/probability of this item being chosen.
    /// Higher values = more likely to be picked.
    /// </summary>
    [DataField]
    public float Weight = 1.0f;

    /// <summary>
    /// Optional list of accompanying items to spawn alongside this item.
    /// For example, a pistol magazine could spawn with the actual pistol.
    /// </summary>
    [DataField]
    public List<EntProtoId>? Accompanying;
}
