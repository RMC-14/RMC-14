using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Blood;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoPlasmaBloodSystem))]
public sealed partial class XenoPlasmaBloodComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<ReagentPrototype>> PlasmaTypes = new();
}
