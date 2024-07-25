using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Acid;

/// <summary>
/// Raised on an entity when a xeno corrodes it with acid.
/// </summary>
[ByRefEvent]
public sealed partial class CorrodingEvent
{
    [DataField]
    public float ExpendableLightDps = 2.5f;

    public CorrodingEvent(float expendableLightDps)
    {
        ExpendableLightDps = expendableLightDps;
    }
}
