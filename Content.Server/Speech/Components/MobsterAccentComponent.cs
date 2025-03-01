namespace Content.Server.Speech.Components;

/// <summary>
///     Nyehh, my gabagool, see?
///     Etc etc.
/// </summary>
[RegisterComponent]
public sealed partial class MobsterAccentComponent : Component
{
    /// <summary>
    ///     Do you make all the rules?
    /// </summary>
    [DataField("isBoss")]
    public bool IsBoss = true;

    [DataField]
    public float PrefixChance = 0.15f;

    [DataField]
    public float SuffixChance = 0.4f;
}
