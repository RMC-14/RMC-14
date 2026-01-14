using Content.Shared.Explosion;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Explosion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedHefaKnightsSystem))]
public sealed partial class HefaKnightExplosionsComponent : Component
{
    [DataField]
    public EntProtoId? ExplosionEffect = "CMExplosionEffectGrenade";

    [DataField]
    public EntProtoId? ShockWaveEffect = "RMCExplosionEffectGrenadeShockWave";

    [DataField]
    public EntProtoId ShrapnelProjectile = "CMProjectileShrapnel";

    [DataField]
    public int ShrapnelCount = 48;

    [DataField]
    public ProtoId<ExplosionPrototype> ExplosionType = "RMC";

    [DataField]
    public float TotalIntensity = 110;

    [DataField]
    public float MaxIntensity = 8;

    [DataField]
    public float IntensitySlope = 4;

    // HEFA Sword
    [DataField, AutoNetworkedField]
    public bool Primed;

    [DataField]
    public SoundSpecifier? PrimeSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/click.ogg");

    [DataField]
    public LocId PrimedPopup = "rmc-hefa-sword-primed";

    [DataField]
    public LocId DeprimedPopup = "rmc-hefa-sword-deprimed";
}
