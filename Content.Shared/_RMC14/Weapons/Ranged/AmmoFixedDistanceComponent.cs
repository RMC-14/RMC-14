using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class AmmoFixedDistanceComponent : Component // TODO: Make it so weapons with this component can have arc fire disabled.
{
    [DataField, AutoNetworkedField]
    public float? MaxRange;
}
