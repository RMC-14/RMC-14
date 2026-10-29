using Content.Shared.Damage;

namespace Content.Shared._RMC14.Hijack;

/// <summary>
///     Marks an entity as a possible random damage target when a hijacked dropship lands.
/// </summary>
[RegisterComponent]
public sealed partial class RMCHijackRandomDamageTargetComponent : Component
{
    /// <summary>
    ///     Whether this entity can currently be selected by the hijack damage system.
    /// </summary>
    [DataField]
    public bool Enabled = true;

    /// <summary>
    ///     Target pool used for percentage selection and outcome handling.
    /// </summary>
    [DataField(required: true)]
    public RMCHijackRandomDamageCategory Category;

    /// <summary>
    ///     Damage target for the non-breaking outcome.
    ///     The system applies only the missing damage needed to reach this total.
    /// </summary>
    [DataField]
    public DamageSpecifier? Damage;

    /// <summary>
    ///     Damage target for the breaking outcome.
    ///     This should be high enough to trigger the prototype's existing destructible flow.
    /// </summary>
    [DataField]
    public DamageSpecifier? BreakDamage;
}

/// <summary>
///     Random hijack damage target pools.
/// </summary>
public enum RMCHijackRandomDamageCategory
{
    Wall,
    Window,
    Windoor,
    Pipe,
}
