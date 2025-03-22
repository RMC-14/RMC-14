namespace Content.Shared.Explosion.Components;

/// <summary>
/// This is an extension of the upstream ScatteringGrenadeComponent
/// </summary>
public sealed partial class ScatteringGrenadeComponent
{
    /// <summary>
    /// Adjust the throw direction, -90 makes the  spread angle start at the front, 90 at the back
    /// </summary>
    [DataField]
    public float DirectionAngle = -90;

    /// <summary>
    /// The angle degrees added to the direction of the grenade content when the scattering grenade is rebounding
    /// </summary>
    [DataField]
    public float ReboundAngle = 180;

    /// <summary>
    /// Then angle in which the grenade contents are possible to be launched at
    /// </summary>
    [DataField]
    public float SpreadAngle = 360;

    /// <summary>
    /// Decides if contained entities get toggled after getting launched
    /// </summary>
    [DataField]
    public bool ToggleContents;

    /// <summary>
    /// Triggers the grenade if it collides with impassable entities
    /// </summary>
    [DataField]
    public bool TriggerOnWallCollide;

    /// <summary>
    /// Triggers the grenade on any direct hit
    /// </summary>
    [DataField]
    public bool DirectHitTrigger;
}
