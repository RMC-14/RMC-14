using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Effects.Buildup;

[Access(typeof(RMCBuildupSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCApplyBuildupOnHitComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<RMCBuildupPrototype> Buildup;

    [DataField, AutoNetworkedField]
    public int Amount = 1;
}
