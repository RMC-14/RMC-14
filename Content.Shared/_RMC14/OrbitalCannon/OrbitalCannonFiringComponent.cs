using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.OrbitalCannon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(OrbitalCannonSystem))]
public sealed partial class OrbitalCannonFiringComponent : Component
{
    [DataField, AutoNetworkedField]
    public Vector2i Coordinates;

    [DataField, AutoNetworkedField]
    public string WarheadName;

    [DataField, AutoNetworkedField]
    public EntityUid Squad;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan StartedAt;

    [DataField, AutoNetworkedField]
    public TimeSpan AlertDelay = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public bool Alerted;

    [DataField, AutoNetworkedField]
    public TimeSpan BeginFireDelay = TimeSpan.FromSeconds(6);

    [DataField, AutoNetworkedField]
    public bool BegunFire;

    [DataField, AutoNetworkedField]
    public TimeSpan FireDelay = TimeSpan.FromSeconds(12);

    [DataField, AutoNetworkedField]
    public bool Fired;

    [DataField, AutoNetworkedField]
    public TimeSpan WarnOneDelay = TimeSpan.FromSeconds(16);

    [DataField, AutoNetworkedField]
    public bool WarnedOne;

    [DataField, AutoNetworkedField]
    public TimeSpan WarnTwoDelay = TimeSpan.FromSeconds(20);

    [DataField, AutoNetworkedField]
    public bool WarnedTwo;

    [DataField, AutoNetworkedField]
    public TimeSpan ImpactDelay = TimeSpan.FromSeconds(24);

    [DataField, AutoNetworkedField]
    public bool Impacted;
}
