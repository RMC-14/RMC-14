using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Weapons.Ranged.Magazine;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCMagazineSystem))]
public sealed partial class RMCMagazineVisualsComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ContainerId = "gun_magazine";
}

[Serializable, NetSerializable]
public enum RMCMagazineVisuals : byte
{
    SlideOpen
}
