using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged.IFF;

/// <summary>
/// Makes the held entity immune to friendly fire
/// The faction should be stored on the entity
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(GunIFFSystem))]
public sealed partial class EntityIFFComponent : Component;
