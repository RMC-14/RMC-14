using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Wounds;

/// <summary>
/// Marks an organic body part as affected by eschar.
/// Formation and removal are managed by SharedWoundsSystem.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedWoundsSystem))]
public sealed partial class EscharComponent : Component
{
}
