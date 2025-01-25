using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.OrbitalCannon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
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
    public TimeSpan LastStepAt;
}
