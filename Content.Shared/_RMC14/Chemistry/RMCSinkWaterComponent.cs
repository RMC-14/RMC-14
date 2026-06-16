using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCSinkWaterSystem))]
public sealed partial class RMCSinkWaterComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype> Reagent = "Water";
}
