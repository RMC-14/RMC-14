using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Visor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(VisorSystem))]
public sealed partial class CycleableVisorComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "RMCActionCycleVisor";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public List<string> Containers = new() { "rmc_visor_one" };

    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<ToolQualityPrototype>> RemoveQuality = new() { "Screwing" };

    [DataField, AutoNetworkedField]
    public int? CurrentVisor;
}
