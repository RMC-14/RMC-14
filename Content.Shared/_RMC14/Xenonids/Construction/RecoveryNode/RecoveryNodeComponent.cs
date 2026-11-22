using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Construction.RecoveryNode;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class RecoveryNodeComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public RecoveryType RecoveryType;

    [DataField, AutoNetworkedField]
    public EntProtoId RecoveryEffect = "RMCEffectHealBusy";

    [DataField, AutoNetworkedField]
    public FixedPoint2 RecoveryAmount = 25;

    [DataField, AutoNetworkedField]
    public float Range = 1.5f;

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextRecoveryAt;

    [DataField]
    public DoAfterId? DoAfter;
}

[Serializable, NetSerializable]
public enum RecoveryType
{
    Health,
    Plasma,
}
