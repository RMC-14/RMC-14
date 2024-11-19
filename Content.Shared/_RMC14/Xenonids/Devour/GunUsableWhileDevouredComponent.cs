using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Devour;

/// <summary>
/// For use on an entity with a <see cref="GunComponent"/>. This allows a gun to be usable when the parent entity is devoured.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoDevourSystem))]
public sealed partial class GunUsableWhileDevouredComponent : Component
{
}
