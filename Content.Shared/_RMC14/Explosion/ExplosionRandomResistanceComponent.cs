using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Explosion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCExplosionSystem))]
public sealed partial class ExplosionRandomResistanceComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Min = 0.02;

    [DataField, AutoNetworkedField]
    public FixedPoint2 Max = 0.1;
}
