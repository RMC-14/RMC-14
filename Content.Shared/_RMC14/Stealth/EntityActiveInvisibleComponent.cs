using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Stealth;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EntityActiveInvisibleComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Opacity = 0.1f;

    /// <summary>
    /// How much "greyer" the invisible entity should become.
    /// 0 means leave saturation the same, 1 means full monochrome.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float GreyFactor;

    /// <summary>
    /// How much "darker" the invisible entity should become.
    /// 0 means do not darken. 1 means all colors should become BLACK.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DarkFactor;

    [DataField, AutoNetworkedField]
    public FixedPoint2 EvasionModifier = 0;

    [DataField, AutoNetworkedField]
    public FixedPoint2 EvasionFriendlyModifier = 0;

    /// <summary>
    /// If the entity should collide with other mobs while invisible.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DisableMobCollision;
}
