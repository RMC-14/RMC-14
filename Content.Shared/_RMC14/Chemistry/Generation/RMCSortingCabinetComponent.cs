using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Chemistry.Generation;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(RMCSortingCabinetSystem))]
public sealed partial class RMCSortingCabinetComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ContainerId = "rmc_sorting_cabinet";

    [DataField, AutoNetworkedField]
    public EntProtoId ReportProto = "RMCChemResearchReport";

    [DataField, AutoNetworkedField]
    public TimeSpan OpenDuration = TimeSpan.FromSeconds(0.5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? CloseAt;

    [DataField, AutoNetworkedField]
    public RMCSortingCabinetSort Sort = RMCSortingCabinetSort.None;

    [DataField, AutoNetworkedField]
    public bool Descending = true;
}
