using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Kitchen
{
    public sealed class ProcessorRecipeManager
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public List<ProcessorRecipePrototype> Recipes { get; private set; } = new();

        public void Initialize()
        {
            Recipes = new List<ProcessorRecipePrototype>();
            foreach (var item in _prototypeManager.EnumeratePrototypes<ProcessorRecipePrototype>())
            {
                if (!item.ProcessorSecretRecipe)
                    Recipes.Add(item);
            }

            Recipes.Sort(new RecipeComparer());
        }
        /// <summary>
        /// Check if a prototype ids appears in any of the recipes that exist.
        /// </summary>
        public bool SolidAppears(string solidId)
        {
            return Recipes.Any(recipe => recipe.IngredientsSolids.ContainsKey(solidId));
        }

        private sealed class RecipeComparer : Comparer<ProcessorRecipePrototype>
        {
            public override int Compare(ProcessorRecipePrototype? x, ProcessorRecipePrototype? y)
            {
                if (x == null || y == null)
                {
                    return 0;
                }

                var nx = x.IngredientCount();
                var ny = y.IngredientCount();
                return -nx.CompareTo(ny);
            }
        }
    }
}
