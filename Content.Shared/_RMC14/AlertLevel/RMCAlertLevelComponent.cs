using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.AlertLevel;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCAlertLevelSystem))]
public sealed partial class RMCAlertLevelComponent : Component
{
    [DataField, AutoNetworkedField]
    public RMCAlertLevels Level = RMCAlertLevels.Green;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? BlueElevatedSound = new SoundPathSpecifier("/Audio/_RMC14/AI/code_blue_elevated.ogg");

    [DataField, AutoNetworkedField]
    public LocId? BlueElevatedMessage = "rmc-alert-level-blue-elevated";

    [DataField, AutoNetworkedField]
    public SoundSpecifier? BlueLoweredSound = new SoundPathSpecifier("/Audio/_RMC14/AI/code_blue_lowered.ogg");

    [DataField, AutoNetworkedField]
    public LocId? BlueLoweredMessage = "rmc-alert-level-blue-lowered";

    [DataField, AutoNetworkedField]
    public SoundSpecifier? GreenSound = new SoundPathSpecifier("/Audio/_RMC14/AI/code_green.ogg");

    [DataField, AutoNetworkedField]
    public LocId? GreenMessage = "rmc-alert-level-green";

    [DataField, AutoNetworkedField]
    public SoundSpecifier? RedElevatedSound = new SoundPathSpecifier("/Audio/_RMC14/AI/code_red_elevated.ogg");

    [DataField, AutoNetworkedField]
    public LocId? RedElevatedMessage = "rmc-alert-level-red-elevated";

    [DataField, AutoNetworkedField]
    public SoundSpecifier? RedLoweredSound = new SoundPathSpecifier("/Audio/_RMC14/AI/code_red_lowered.ogg");

    [DataField, AutoNetworkedField]
    public LocId? RedLoweredMessage = "rmc-alert-level-red-lowered";

    [DataField, AutoNetworkedField]
    public LocId? DeltaAnnouncement = "rmc-announcement-delta";

    // TODO RMC14
    [DataField, AutoNetworkedField]
    public SoundSpecifier? DeltaSound = new SoundPathSpecifier("/Audio/Misc/gamma.ogg");

    [DataField, AutoNetworkedField]
    public ProtoId<RadioChannelPrototype> RadioChannel = "MarineCommon";
}
