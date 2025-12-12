using System.Numerics;

// Resharper disable once CheckNameSpace
namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
///     Extension of the upstream AmmoComponent.
/// </summary>
public partial class AmmoComponent
{
    [DataField]
    public Vector2? MuzzleFlashOffset;
}
