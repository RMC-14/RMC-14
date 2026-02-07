using System.Numerics;

// Resharper disable once CheckNameSpace
namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
///     Extension of the upstream AmmoComponent.
/// </summary>
public partial class AmmoComponent
{
    /// <summary>
    ///     Distance the muzzle flash rotates from its origin.
    /// </summary>
    [DataField]
    public Vector2 MuzzleFlashOffset = new (0.5f, 0);
}
