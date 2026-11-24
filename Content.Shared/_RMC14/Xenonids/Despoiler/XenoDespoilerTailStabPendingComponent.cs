using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Despoiler;

/// <summary>
///     One-shot marker set during RMCGetTailStabBonusDamageEvent and consumed by the
///     subsequent MeleeHitEvent so finishing-stab bonus damage is applied exactly once
///     to the entity that triggered the tail stab.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class XenoDespoilerTailStabPendingComponent : Component;
