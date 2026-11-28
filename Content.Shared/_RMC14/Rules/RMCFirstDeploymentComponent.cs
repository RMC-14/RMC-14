using Content.Shared._RMC14.Announce;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Rules;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class RMCFirstDeploymentComponent : Component
{
    [DataField]
    public EntProtoId<RMCAnnouncementPresetComponent> Overlay = "RMCAnnouncementFirstDeployment";

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? AnnounceAt;

    [DataField]
    public bool Announced;

    [DataField]
    public string? WarshipName;

    [DataField]
    public LocId DefaultAssignment = "rmc-announcement-first-deployment-assignment-crew";
}
