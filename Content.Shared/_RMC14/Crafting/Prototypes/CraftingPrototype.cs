using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Crafting.Prototypes;
/// <summary>
/// Prototype to handle by <see cref="SharedCraftingSystem"/>
/// </summary>
[Prototype("craftRecipe"), Serializable, NetSerializable]
public sealed class CraftingPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;
    /// <summary>
    /// Crafting result prototype ID
    /// </summary>
    [DataField("resultProtos")]
    public List<string> ResultProtos = new();

    /// <summary>
    /// DoAfter time for crafting
    /// </summary>
    [DataField("craftTime")]
    public float CraftTime = 2f;

    /// <summary>
    /// Chance for disassemble
    /// </summary>
    [DataField]
    public float DisassembleChance = 1f;
    /// <summary>
    /// Items required for crafting.
    /// This supports multiple items with details at once.
    /// </summary>
    [DataField("items")]
    public Dictionary<string, CraftingRecipeDetails> Items = new();

    /// <summary>
    /// Optional field to specify the workbench EntProtoId required for this recipe.
    /// If not provided, the recipe can be crafted on any workbench.
    /// </summary>
    [DataField("requiredWorkbench")]
    public string? RequiredWorkbench;
}
/// <summary>
/// Details for crafting recipe item
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class CraftingRecipeDetails
{
    /// <summary>
    /// Amount of items of the type to craft
    /// </summary>
    [DataField("amount")]
    public int Amount;
    /// <summary>
    /// If this item is a catalyzer, it wont be consumed by crafting.
    /// </summary>
    [DataField("catalyzer")]
    public bool Catalyzer;

    [DataField("tag")]
    public bool Tag;
}
