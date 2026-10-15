using Content.Shared.Damage;
using Content.Shared.Explosion;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Explosion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCExplosiveDamageOnHitComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<RMCExplosiveDamageOnHit> Explosions = new();
}

[DataDefinition]
[Serializable, NetSerializable]
public partial struct RMCExplosiveDamageOnHit()
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public ProtoId<ExplosionPrototype> ExplosionType = "RMC";

    [DataField]
    public DamageSpecifier Damage = new();

    [DataField]
    public int ArmorPiercing = 0;
}
