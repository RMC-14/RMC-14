// ReSharper disable CheckNamespace
namespace Content.Shared.Cloning;

public sealed partial class CloningSettingsPrototype
{
    /// <summary>
    ///     Whether to copy as many components as possible and use the exclusion list instead
    /// </summary>
    [DataField]
    public bool CopyAll = false;

    /// <summary>
    ///     Components to NOT copy from the original to the clone.
    ///     Used in conjunction with CopyAll to specify wich components shouldn't be copied.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public HashSet<string> ExcludeComponents = new();
}
