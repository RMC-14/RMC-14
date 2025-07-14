using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Weapons.Ranged.Magazine;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCMagazineSystem))]
public sealed partial class RMCMagazineVisualsComponent : Component;

[Serializable, NetSerializable]
public enum RMCMagazineVisuals : byte
{
    SlideOpen
}
