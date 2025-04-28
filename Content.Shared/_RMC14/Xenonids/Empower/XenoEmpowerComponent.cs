using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Empower;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoEmpowerComponent : Component
{
    [DataField, AutoNetworkedField]
    public int MaxTargets = 6;

    [DataField, AutoNetworkedField]
    public int SuperThreshold = 3;

    [DataField, AutoNetworkedField]
    public FixedPoint2 Cost = 50;

    [DataField]
    public float Range = 4;

    [DataField]
    public int InitialShield = 50;

    [DataField]
    public int ShieldPerTarget = 50;

    [DataField, AutoNetworkedField]
    public bool ActivatedOnce = false;

    [DataField]
    public TimeSpan TimeoutDuration = TimeSpan.FromSeconds(6);

    [DataField]
    public TimeSpan? TimeoutAt;

    [DataField]
    public TimeSpan FirstActivationAt;

    [DataField]
    public TimeSpan CooldownDuration = TimeSpan.FromSeconds(18);

    [DataField, AutoNetworkedField]
    public DamageSpecifier DamageIncreasePer = new();

    [DataField, AutoNetworkedField]
    public DamageSpecifier DamageTailIncreasePer = new();

    [DataField]
    public TimeSpan ShieldDecayTime = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan? ShieldDecayAt;

    [DataField]
    public TimeSpan SuperEmpowerPartialDuration = TimeSpan.FromSeconds(5);

    [DataField]
    public Color SuperEmpowerColor = Color.FromHex("#FF000046");

    [DataField]
    public EntProtoId TargetEffect = "RMCEffectXenoTelegraphRedEmpower";

    [DataField]
    public EntProtoId EmpowerEffect = "RMCEffectEmpower";

    [DataField]
    public ProtoId<EmotePrototype> RoarEmote = "XenoRoar";

    [DataField]
    public ProtoId<EmotePrototype> TailEmote = "XenoTailSwipe";

    [DataField, AutoNetworkedField]
    public DamageSpecifier LeapDamage = new();
}
