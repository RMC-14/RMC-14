using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Empower;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoSuperEmpoweredComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan PartialExpireAt;

    [DataField, AutoNetworkedField]
    public TimeSpan ExpireTime = TimeSpan.FromSeconds(1.5);

    [DataField]
    public Color FadingEmpowerColor = Color.FromHex("#FF000023");

    [DataField, AutoNetworkedField]
    public TimeSpan? ExpiresAt;

    [DataField]
    public int EmpoweredTargets;

    [DataField, AutoNetworkedField]
    public DamageSpecifier DamageIncreasePer = new();

    [DataField, AutoNetworkedField]
    public DamageSpecifier DamageTailIncreasePer = new();

    [DataField, AutoNetworkedField]
    public DamageSpecifier LeapDamage = new();

    [DataField, AutoNetworkedField]
    public float FlingDistance = 1.75f; // 3 tiles from start

    [DataField, AutoNetworkedField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(3.2);
}
