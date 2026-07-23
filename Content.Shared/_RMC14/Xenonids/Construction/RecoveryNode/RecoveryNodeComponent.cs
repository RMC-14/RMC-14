using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Construction.RecoveryNode;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RecoveryNodeComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public RecoveryType RecoveryType;

    [DataField, AutoNetworkedField]
    public FixedPoint2 RecoveryAmount = 25;

    [DataField, AutoNetworkedField]
    public float Range = 1.5f;

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan NextRecoveryAt;

    [DataField]
    public DoAfterId? DoAfter;
}

public enum RecoveryType
{
    Health,
    Plasma,
}
