namespace Content.Shared.Light.Components;

/// <summary>
///     This is an extension of the upstream ExpendableLightComponent
/// </summary>
public sealed partial class ExpendableLightComponent
{
    /// <summary>
    ///     Can the expendable light be picked up while it's turned on.
    /// </summary>
    [DataField]
    public bool PickupWhileOn = true;
}
