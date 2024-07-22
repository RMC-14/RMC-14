using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Physics;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.OnCollide;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedOnCollideSystem))]
public sealed partial class DamageOnCollideComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool InitDamaged;

    [DataField, AutoNetworkedField]
    public EntityUid? Chain;

    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    [DataField(required: true)]
    public DamageSpecifier ChainDamage = new();

    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Damaged = new();

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public bool DamageDead;

    [DataField]
    public ProtoId<EmotePrototype>? Emote = "Scream";

    [DataField]
    public CollisionGroup Collision = CollisionGroup.FullTileLayer;

    [DataField]
    public bool ReduceChainDamage;
}
