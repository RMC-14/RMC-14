using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Doors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(CMDoorSystem))]
public sealed partial class RMCDoorButtonComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? Id;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastUse;

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(0.5);

    [DataField, AutoNetworkedField]
    public string OffState = "doorctrl";

    [DataField, AutoNetworkedField]
    public string OnState = "doorctrl1";

    [DataField, AutoNetworkedField]
    public string DeniedState = "doorctrl-denied";

    [DataField, AutoNetworkedField]
    public TimeSpan? MinimumRoundTimeToPress;

    [DataField, AutoNetworkedField]
    public bool Used = false;

    [DataField, AutoNetworkedField]
    public bool UseOnlyOnce = false;

    [DataField, AutoNetworkedField]
    public LocId NoTimeMessage = "rmc-machines-button-cannot-be-lifted-weya";

    [DataField, AutoNetworkedField]
    public LocId AlreadyUsedMessage = "rmc-machines-button-already-lifted-weya";

    [DataField, AutoNetworkedField]
    public LocId? MarineAnnouncement;

    [DataField, AutoNetworkedField]
    public LocId MarineAnnouncementAuthor = "rmc-announcement-author";
}
