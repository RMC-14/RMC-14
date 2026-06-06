namespace Content.Server._RMC14.Xenonids.Despoiler;

/// <summary>
///     One-shot marker set during RMCGetTailStabBonusDamageEvent and consumed by the
///     subsequent MeleeHitEvent so finishing-stab bonus damage is applied exactly once
///     to the entity that triggered the tail stab.
/// </summary>
[RegisterComponent]
public sealed partial class XenoDespoilerTailStabPendingComponent : Component;
