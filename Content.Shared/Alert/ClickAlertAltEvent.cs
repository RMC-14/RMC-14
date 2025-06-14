using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Alert;

/// <summary>
///     A message that calls the alt click interaction on an alert
/// </summary>
[Serializable, NetSerializable]
public sealed class ClickAlertAltEvent : EntityEventArgs
{
    public readonly ProtoId<AlertPrototype> Type;

    public ClickAlertAltEvent(ProtoId<AlertPrototype> alertType)
    {
        Type = alertType;
    }
}
