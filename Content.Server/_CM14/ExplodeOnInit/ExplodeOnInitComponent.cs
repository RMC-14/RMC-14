namespace Content.Server._CM14.ExplodeOnInit;

[RegisterComponent]
public sealed partial class ExplodeOnInitComponent : Component
{
    /// <summary>
    /// I know this sounds stupid. Toggles whether it should explode on init or use a timer.
    /// </summary>
    [DataField] public bool ExplodeOnInit = true;

    [DataField] public float TimeUntilDetonation = 1f;
}
