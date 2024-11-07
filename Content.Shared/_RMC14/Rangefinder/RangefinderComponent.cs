using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Rangefinder;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(RangefinderSystem))]
public sealed partial class RangefinderComponent : Component
{
    [DataField, AutoNetworkedField]
    public int? Id;

    [DataField, AutoNetworkedField]
    public int Range = 25;

    [DataField, AutoNetworkedField]
    public bool CanDesignate;

    [DataField, AutoNetworkedField]
    public RangefinderMode Mode;

    [DataField, AutoNetworkedField]
    public Vector2i? LastTarget;

    [DataField, AutoNetworkedField]
    public string SwitchModeUseDelay = "rangefinder_mode";

    [DataField, AutoNetworkedField]
    public TimeSpan SwitchModeDelay = TimeSpan.FromSeconds(0.5);

    [DataField, AutoNetworkedField]
    public DoAfter.DoAfter? DoAfter;

    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public TimeSpan MinimumDelay = TimeSpan.FromSeconds(1.5);

    [DataField, AutoNetworkedField]
    public TimeSpan TimePerSkillLevel = TimeSpan.FromSeconds(2.5);

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillJtac";

    [DataField, AutoNetworkedField]
    public EntProtoId RangefinderSpawn = "RMCRangefinderTarget";

    [DataField, AutoNetworkedField]
    public EntProtoId<LaserDesignatorTargetComponent> DesignatorSpawn = "RMCLaserDesignatorTarget";

    [DataField, AutoNetworkedField]
    public SoundSpecifier? TargetSound = new SoundPathSpecifier("/Audio/_RMC14/Binoculars/nightvision.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? AcquireSound = new SoundPathSpecifier("/Audio/_RMC14/Binoculars/binoctarget.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ToggleSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/click.ogg");
}

[Serializable, NetSerializable]
public enum RangefinderLayers
{
    Layer,
}

[Serializable, NetSerializable]
public enum RangefinderMode
{
    Rangefinder,
    Designator,
}
