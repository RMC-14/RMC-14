namespace Content.Shared._RMC14.Kitchen
{
    /// <summary>
    /// This returns a list of recipes not found in the main list of available recipes.
    /// </summary>
    [ByRefEvent]
    public struct GetSecretProcessorRecipesEvent()
    {
        public List<ProcessorRecipePrototype> Recipes = new();
    }
}
