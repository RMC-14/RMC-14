using Content.Shared._RMC14.Medical.MedivacStretcher;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship.Weapon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access([typeof(SharedDropshipWeaponSystem), typeof(SharedMedivacStretcherSystem)])]
public sealed partial class DropshipTargetComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Abbreviation = string.Empty;

    [DataField, AutoNetworkedField]
    public bool IsTargetableByWeapons = true;
}
