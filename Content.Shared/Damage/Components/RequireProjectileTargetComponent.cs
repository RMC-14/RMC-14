using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Prevent the object from getting hit by projetiles unless you target the object.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RequireProjectileTargetSystem))]
public sealed partial class RequireProjectileTargetComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active = true;

    /// <summary>
    /// When true, object is hit if the targeted coordinates collides with this object.
    /// Inefficent, avoid setting to true for a commonly placed entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CollideOnTargetCoords = false;
}
