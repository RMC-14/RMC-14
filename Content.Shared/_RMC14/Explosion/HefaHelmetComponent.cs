using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Explosion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedHefaKnightsExplosionSystem))]
public sealed partial class HefaHelmetComponent : Component
{
    [DataField]
    public EntProtoId ShrapnelPrototype = "CMProjectileShrapnel";

    [DataField]
    public int ShrapnelCount = 48;

    [DataField]
    public float SpreadAngle = 360f;

    [DataField]
    public float ProjectileSpeed = 20f;

    [DataField]
    public float MinVelocity = 2f;

    [DataField]
    public float MaxVelocity = 6f;

    /// <summary>
    /// The entity wearing this helmet, tracked for proper explosion origin.
    /// Set when equipped, cleared when unequipped.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Wearer;
}
