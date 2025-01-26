using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.SupplyDrop;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedSupplyDropSystem))]
public sealed partial class BeingSupplyDroppedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityCoordinates Target;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan ArrivingSoundAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan DropAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan OpenAt;

    [DataField, AutoNetworkedField]
    public EntityUid? LandingEffect;

    [DataField, AutoNetworkedField]
    public DamageSpecifier? LandingDamage;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ArrivingSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/Techpod/techpod_drill.ogg");

    [DataField, AutoNetworkedField]
    public bool PlayedArrivingSound;

    [DataField, AutoNetworkedField]
    public bool Landed;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? LandSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/Techpod/techpod_hit.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? OpenSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/Techpod/techpod_open.ogg");
}
