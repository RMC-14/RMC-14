using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.AcidShroud;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoAcidShroudSystem))]
public sealed partial class XenoAcidShroudComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan DoAfter = TimeSpan.FromSeconds(0.75);

    [DataField, AutoNetworkedField]
    public EntProtoId Spawn = "RMCSmokeAcidShroud";

    [DataField, AutoNetworkedField]
    public EntProtoId[] Gases = new[]
{
        new EntProtoId("RMCSmokeAcidShroud"),
        new EntProtoId("RMCSmokeNeurotoxinShroud"),
    };
}
