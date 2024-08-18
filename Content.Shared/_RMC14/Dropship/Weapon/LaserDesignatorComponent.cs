using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Dropship.Weapon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDropshipWeaponSystem))]
public sealed partial class LaserDesignatorComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Id;

    [DataField, AutoNetworkedField]
    public int Range = 25;

    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public TimeSpan MinimumDelay = TimeSpan.FromSeconds(1.5);

    [DataField, AutoNetworkedField]
    public TimeSpan TimePerSkillLevel = TimeSpan.FromSeconds(2.5);

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillJtac";

    [DataField, AutoNetworkedField]
    public EntProtoId<LaserDesignatorTargetComponent> TargetSpawn = "RMCLaserDesignatorTarget";

    [DataField, AutoNetworkedField]
    public SoundSpecifier? TargetSound = new SoundPathSpecifier("/Audio/_RMC14/Binoculars/nightvision.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? AcquireSound = new SoundPathSpecifier("/Audio/_RMC14/Binoculars/binoctarget.ogg");
}
