using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Evasion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(EvasionSystem))]
public sealed partial class EvasionComponent : Component
{
    /// <summary>
    /// Base evasion value.
    /// Conversion from 13: evasion
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Evasion = 0;

    /// <summary>
    /// Evasion value after applicable modifiers. This is subtracted from the hit chance of most incoming projectiles.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 ModifiedEvasion = 0;

    /// <summary>
    /// Base evasion value for friendly fire.
    /// Conversion from 13: FF_hit_evade
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 EvasionFriendly = 15;

    /// <summary>
    /// Evasion value after applicable modifiers. This is subtracted from the hit chance of most incoming friendly projectiles.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 ModifiedEvasionFriendly = 0;
}

public enum EvasionModifiers : int
{
    Rest = -15,
    Invisibility = 1000,
    SizeSmall = 10,
    SizeBig = -10
}
