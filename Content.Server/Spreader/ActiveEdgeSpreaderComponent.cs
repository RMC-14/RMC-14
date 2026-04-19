using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Spreader;

/// <summary>
/// Added to entities being considered for spreading via <see cref="SpreaderSystem"/>.
/// This needs to be manually added and removed.
/// </summary>
[RegisterComponent]
public sealed partial class ActiveEdgeSpreaderComponent : Component
{
    /// RMC14
    /// <summary>
    /// Game time for the next spread.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextSpread = TimeSpan.Zero;
}
