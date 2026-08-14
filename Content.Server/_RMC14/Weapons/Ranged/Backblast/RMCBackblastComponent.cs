using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Weapons.Ranged.Backblast;

[RegisterComponent]
public sealed partial class RMCBackblastComponent : Component
{
    [DataField(required: true)]
    public EntProtoId NearEffect;

    [DataField(required: true)]
    public EntProtoId FarEffect;

    [DataField]
    public float KnockbackDistance = 2;

    [DataField]
    public float KnockbackSpeed = 25;

    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan DeafTime = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan StutterTime = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan DizzyTime = TimeSpan.FromSeconds(2);
}
