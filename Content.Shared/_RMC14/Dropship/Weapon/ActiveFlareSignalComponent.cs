using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Dropship.Weapon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDropshipWeaponSystem))]
public sealed partial class ActiveFlareSignalComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? Abbreviation;

    [DataField, AutoNetworkedField]
    public Queue<NetCoordinates> LastCoordinates = new();
}
