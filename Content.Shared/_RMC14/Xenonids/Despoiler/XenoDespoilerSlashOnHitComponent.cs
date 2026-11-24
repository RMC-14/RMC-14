using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Despoiler;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoDespoilerSlashOnHitComponent : Component
{
    [DataField, AutoNetworkedField]
    public int EnhanceStacksThreshold = 2;

    [DataField, AutoNetworkedField]
    public ProtoId<DamageTypePrototype> DamageType = "Heat";
}
