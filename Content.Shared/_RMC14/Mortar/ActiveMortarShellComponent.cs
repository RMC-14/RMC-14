using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Mortar;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedMortarSystem))]
public sealed partial class ActiveMortarShellComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityCoordinates Coordinates;

    [DataField, AutoNetworkedField]
    public TimeSpan WarnAt;

    [DataField, AutoNetworkedField]
    public bool Warned;

    [DataField, AutoNetworkedField]
    public float WarnRange = 15;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? WarnSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/gun_mortar_travel.ogg");

    [DataField, AutoNetworkedField]
    public TimeSpan ImpactWarnAt;

    [DataField, AutoNetworkedField]
    public bool ImpactWarned;

    [DataField, AutoNetworkedField]
    public float ImpactWarnRange = 10;

    [DataField, AutoNetworkedField]
    public TimeSpan LandAt;
}
