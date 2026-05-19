// ReSharper disable CheckNamespace
namespace Content.Shared.Cloning;

public sealed partial class CloningSettingsPrototype
{
    /// <summary>
    ///     Components to NOT copy from the original to the clone.
    ///     For systems that use dynamic/internal methods of determining which components to clone.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public HashSet<string> ExcludeComponents = new();
}
