namespace Content.Shared._RMC14.Projectiles;

[RegisterComponent]
public sealed partial class RMCAreaDamageComponent : Component
{
    /// <summary>
    ///     The range in which damage is dealt.
    /// </summary>
    [DataField]
    public float DamageArea;

    /// <summary>
    ///     The range after which damage starts to fall off.
    /// </summary>
    [DataField]
    public float FalloffRange = 0.5f;
}
