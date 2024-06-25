using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class AmmoFixedDistanceComponent : Component
{
    /// <summary>
    /// If set to true, this makes shots from the weapon pass over creatures and objects.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HighArc = false;
}
