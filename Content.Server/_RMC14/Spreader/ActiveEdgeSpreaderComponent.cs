using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

// ReSharper disable CheckNamespace
namespace Content.Server.Spreader;

public sealed partial class ActiveEdgeSpreaderComponent : Component
{
    /// <summary>
    /// Game time for the next spread.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextSpread = TimeSpan.Zero;
}
