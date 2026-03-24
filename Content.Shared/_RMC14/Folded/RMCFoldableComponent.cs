// ReSharper disable CheckNamespace
namespace Content.Shared.Foldable;
// ReSharper enable CheckNamespace

public sealed partial class FoldableComponent
{
    [DataField, AutoNetworkedField]
    public bool AnchorOnUnfold;

    [DataField, AutoNetworkedField]
    public bool EnableStrapOnUnfold = true;

    /// <summary>
    ///     Determines if it's possible to fold/unfold the foldable.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsLocked;
}
