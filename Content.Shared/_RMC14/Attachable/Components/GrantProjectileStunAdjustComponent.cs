using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GrantProjectileStunAdjustComponent : Component
{
    [DataField, AutoNetworkedField]
    public float StunDurationAdjustment = 1;

    [DataField, AutoNetworkedField]
    public float DazeDurationAdjustment = 1;

    [DataField, AutoNetworkedField]
    public float MaxRangeAdjustment = 1;

    [DataField, AutoNetworkedField]
    public bool ForceKnockBackAdjustment;

    [DataField, AutoNetworkedField]
    public float KnockBackPowerMinAdjustment = 1;

    [DataField, AutoNetworkedField]
    public float KnockBackPowerMaxAdjustment = 1;

    [DataField, AutoNetworkedField]
    public bool LosesEffectWithRangeAdjustment;

    [DataField, AutoNetworkedField]
    public bool SlowsEffectBigXenosAdjustment;

    [DataField, AutoNetworkedField]
    public float SuperSlowTimeAdjustment = 1;

    [DataField, AutoNetworkedField]
    public float SlowTimeAdjustment = 1;

    [DataField, AutoNetworkedField]
    public float StunAreaAdjustment = 1;
}
