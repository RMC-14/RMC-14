using Content.Shared._RMC14.Medical.MedevacStretcher;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship.Weapon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access([typeof(SharedDropshipWeaponSystem), typeof(MedevacStretcherSystem)])]
public sealed partial class DropshipTargetComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Abbreviation = string.Empty;

    [DataField, AutoNetworkedField]
    public bool IsTargetableByWeapons = true;

    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, EntityUid> Eyes = new();
}
