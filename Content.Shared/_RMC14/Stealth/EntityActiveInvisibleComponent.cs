using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Stealth;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class EntityActiveInvisibleComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Opacity = 0.1f;

    [DataField, AutoNetworkedField]
    public FixedPoint2 EvasionModifier = 0;

    [DataField, AutoNetworkedField]
    public FixedPoint2 EvasionFriendlyModifier = 1000;

    /// <summary>
    /// If the entity should collide with other mobs while invisible.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DisableMobCollision;
}
