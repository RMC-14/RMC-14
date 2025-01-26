using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.OrbitalCannon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(OrbitalCannonSystem))]
public sealed partial class OrbitalCannonWarheadComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId<OrbitalCannonExplosionComponent> Explosion;
}
