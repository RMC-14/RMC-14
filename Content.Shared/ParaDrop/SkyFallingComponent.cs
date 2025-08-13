using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.ParaDrop;

[RegisterComponent, NetworkedComponent]
public sealed partial class SkyFallingComponent : Component
{
    /// <summary>
    ///     The remaining duration of the animation.
    /// </summary>
    [DataField]
    public float RemainingTime = 1.5f;

    /// <summary>
    ///     The original scale of the entity so it can be restored
    /// </summary>
    [DataField]
    public Vector2 OriginalScale;

    /// <summary>
    ///     Scale that the animation should bring entities to.
    /// </summary>
    [DataField]
    public Vector2 AnimationScale = new (0.01f, 0.01f);

    /// <summary>
    ///     The location the entity should be teleported to after the animation is done.
    /// </summary>
    [DataField]
    public EntityCoordinates? TargetCoordinates;
}
