using Content.Shared.Actions;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Brutalize;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoBrutalizeSystem))]
public sealed partial class XenoBrutalizeComponent : Component
{
    [DataField, AutoNetworkedField]
    public int? MaxTargets;

    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new();

    [DataField, AutoNetworkedField]
    public EntProtoId Effect = "RMCEffectExtraSlash";

    [DataField, AutoNetworkedField]
    public float Range = 1.5f;

    [DataField, AutoNetworkedField]
    public TimeSpan BaseCooldownReduction = TimeSpan.FromSeconds(1.5);

    [DataField, AutoNetworkedField]
    public TimeSpan AddtionalCooldownReductions = TimeSpan.FromSeconds(0.5);
}
