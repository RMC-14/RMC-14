using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class ProjectileFixedDistanceComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan FlyEndTime;
}
