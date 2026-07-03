using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Projectile.Spit.Charge;

[RegisterComponent, NetworkedComponent]
public sealed partial class XenoAcidTypeComponent : Component
{
    [DataField]
    public DamageSpecifier Damage = new();

    [DataField]
    public int Tier = 1;

    //TODO RMC14 Damage areas

    [DataField]
    public TimeSpan[] MultiplierThresholds = [TimeSpan.FromSeconds(21), TimeSpan.FromSeconds(9), TimeSpan.FromSeconds(20)];

    [DataField]
    public int ArmorPiercing;

    [DataField]
    public TimeSpan DurationBase = TimeSpan.FromSeconds(20);

    [DataField]
    public TimeSpan DurationAdd = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan MaxDuration = TimeSpan.FromSeconds(20);

    [DataField]
    public EntProtoId? Upgrade;

    [DataField]
    public UserAcidedEffects Appearance;

    [DataField]
    public int WeakenArmor = 0;
}
