using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged.IFF;

[RegisterComponent, NetworkedComponent]
[Access(typeof(GunIFFSystem))]
public sealed partial class MarineIFFComponent : Component;
