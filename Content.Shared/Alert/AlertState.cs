using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Alert;

[Serializable, NetSerializable]
public struct AlertState
{
    public short? Severity;
    public (TimeSpan, TimeSpan)? Cooldown;
    public string? DynamicMessage;
    public bool AutoRemove;
    public bool ShowCooldown;
    public ProtoId<AlertPrototype> Type;
}
