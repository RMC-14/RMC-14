using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Destroy;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoDestroyComponent : Component
{
    [DataField, AutoNetworkedField]
    public DamageSpecifier StructureDamage = new();

    [DataField, AutoNetworkedField]
    public EntProtoId Telegraph = "RMCEffectXenoTelegraphKing";
}
