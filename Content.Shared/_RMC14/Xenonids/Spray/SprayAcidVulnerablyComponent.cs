using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Spray;

/// <summary>
/// Marks an entity as vulnerable to xeno spray acid.
/// Used by <see cref="XenoSprayAcidSystem"/> to determine
/// whether SprayAcidedComponent should be applied.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SprayAcidVulnerableComponent : Component
{
}