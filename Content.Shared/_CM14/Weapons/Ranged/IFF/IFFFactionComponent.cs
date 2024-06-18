using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Weapons.Ranged.IFF;

[RegisterComponent, NetworkedComponent]
[Access(typeof(GunIFFSystem))]
public sealed partial class IFFFactionComponent : Component;
