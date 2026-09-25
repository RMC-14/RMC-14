// ReSharper disable CheckNamespace
namespace Content.Server.Spreader;

public sealed partial class EdgeSpreaderComponent : Component
{
    /// <summary>
    /// Time between spreads.
    /// </summary>
    [DataField]
    public TimeSpan SpreadDelay = TimeSpan.FromSeconds(1);
}
