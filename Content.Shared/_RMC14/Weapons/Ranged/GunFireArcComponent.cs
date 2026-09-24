using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GunFireArcComponent : Component
{
    [DataField, AutoNetworkedField]
    public Angle Arc = Angle.FromDegrees(90);

    [DataField, AutoNetworkedField]
    public Angle AngleOffset = Angle.Zero;
}
