using Robust.Shared.Prototypes;
using Content.Shared.Crafting.Prototypes;

namespace Content.Shared._RMC14.Crafting.Components
{
    [RegisterComponent]
    public sealed partial class CraftingBlueprintComponent : Component
    {
        [DataField("craftingBlueprint")]
        public ProtoId<CraftingPrototype>? BlueprintId = null;
    }
}
