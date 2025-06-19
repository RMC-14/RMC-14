using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Kitchen.Components
{
    [RegisterComponent]
    public sealed partial class ProcessorRecipeProviderComponent : Component
    {
        /// <summary>
        /// These are additional recipes that the entity is capable of cooking.
        /// </summary>
        [DataField, ViewVariables]
        public List<ProtoId<ProcessorRecipePrototype>> ProvidedRecipes = new();
    }
}
