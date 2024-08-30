using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Brutalize;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoBrutalizeSystem))]
public sealed partial class XenoBrutalizeComponent : Component
{
    [DataField, AutoNetworkedField]
    public int MaxTargets = 8;

    [DataField, AutoNetworkedField]
    public float AOEDamageMult = 0.4f;

    [DataField, AutoNetworkedField]
    public EntProtoId Effect = "RMCEffectExtraSlash";

    [DataField, AutoNetworkedField]
    public float Range = 1;

    [DataField, AutoNetworkedField]
    public TimeSpan BaseCooldownReduction = TimeSpan.FromSeconds(1.5);

    [DataField, AutoNetworkedField]
    public TimeSpan AddtionalCooldownReductions = TimeSpan.FromSeconds(0.5);

    [DataField, AutoNetworkedField]
    public string CummulativeCooldownAction = "ActionXenoCharge";

    [DataField, AutoNetworkedField]
    public string BaseCooldownAction = "ActionXenoDefensiveShield";
}
