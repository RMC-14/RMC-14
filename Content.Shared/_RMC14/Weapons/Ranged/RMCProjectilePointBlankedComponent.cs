namespace Content.Shared._RMC14.Weapons.Ranged;

/// <summary>
///     Added by the client to mark something as having point-blanked
///     Read by the server to check if the client thinks something is a point-blank
/// </summary>
[RegisterComponent]
[Access(typeof(CMGunSystem))]
public sealed partial class RMCProjectilePointBlankedComponent : Component;
