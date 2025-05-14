using Robust.Shared.GameStates;
using Robust.Shared.Toolshed.Commands.Math;

namespace Content.Shared.DoAfter;

/// <summary>
///     Added to entities that are currently performing any doafters.
/// </summary>
[RegisterComponent]
public sealed partial class ActiveDoAfterComponent : Component
{
    [DataField("RootMob")]
    public bool RootMob = false;
}
