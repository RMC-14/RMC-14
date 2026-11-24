using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Despoiler;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoDespoilerAcidBarrageActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public float MaxChargeSeconds = 3f;

    [DataField, AutoNetworkedField]
    public int MinProjectiles = 1;

    [DataField, AutoNetworkedField]
    public int MaxProjectiles = 8;

    [DataField, AutoNetworkedField]
    public int EmpowerBonusProjectiles = 6;

    [DataField, AutoNetworkedField]
    public float ScatterDegrees = 30f;

    [DataField, AutoNetworkedField]
    public float ChargingSpeedMultiplier = 0.5f;

    [DataField, AutoNetworkedField]
    public EntProtoId<XenoDespoilerAcidBarrageProjectileComponent> ProjectileId = "RMCProjectileDespoilerAcidShot";

    [DataField, AutoNetworkedField]
    public float LingeringAcidChance = 0.25f;

    [DataField, AutoNetworkedField]
    public float ProjectileSpeed = 12f;

    [DataField, AutoNetworkedField]
    public int MinRangeTiles = 1;

    [DataField, AutoNetworkedField]
    public int MaxRangeTiles = 6;

    [DataField, AutoNetworkedField]
    public float MinProjectileScale = 0.9f;

    [DataField, AutoNetworkedField]
    public float MaxProjectileScale = 1.33f;

    [DataField, AutoNetworkedField]
    public TimeSpan PostFireCooldown = TimeSpan.FromSeconds(12);

    [DataField, AutoNetworkedField]
    public TimeSpan ChargeGracePeriod = TimeSpan.FromSeconds(30);

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ChargeSound;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? FireSound;
}
