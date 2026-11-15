using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.SupplyDrop;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSupplyDropSystem))]
public sealed partial class CanBeSupplyDroppedComponent : Component
{
    [DataField, AutoNetworkedField]
    public SoundSpecifier? LaunchSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/bamf.ogg");

    [DataField, AutoNetworkedField]
    public TimeSpan ArrivingSoundDelay = TimeSpan.FromSeconds(9);

    [DataField, AutoNetworkedField]
    public TimeSpan DropDuration = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan OpenDelay = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public EntProtoId LandingEffectId = "RMCEffectAlert";

    [DataField, AutoNetworkedField]
    public DamageSpecifier? LandingDamage;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ArrivingSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/Techpod/techpod_drill.ogg");
}
