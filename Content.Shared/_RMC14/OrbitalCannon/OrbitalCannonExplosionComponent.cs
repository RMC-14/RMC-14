using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.OrbitalCannon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, EntityCategory("Spawner")]
[Access(typeof(OrbitalCannonSystem))]
public sealed partial class OrbitalCannonExplosionComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Laser;

    [DataField, AutoNetworkedField]
    public List<OrbitalCannonExplosion> Steps = new();

    [DataField, AutoNetworkedField]
    public int Current;

    [DataField, AutoNetworkedField]
    public TimeSpan LastAt;

    [DataField, AutoNetworkedField]
    public int Step;

    [DataField, AutoNetworkedField]
    public TimeSpan LastStepAt;
}
