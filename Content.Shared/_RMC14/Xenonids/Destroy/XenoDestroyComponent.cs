using Content.Shared._RMC14.Maths;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Explosion;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Destroy;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoDestroyComponent : Component
{
    [DataField, AutoNetworkedField]
    public DamageSpecifier StructureDamage = new();

    [DataField, AutoNetworkedField]
    public DamageSpecifier MobDamage = new();

    [DataField, AutoNetworkedField]
    public bool Gibs = true; //If false mobs will take the mob damage instead (no resist)

    [DataField, AutoNetworkedField]
    public EntProtoId Telegraph = "RMCEffectXenoTelegraphKing";

    [DataField, AutoNetworkedField]
    public float Range = 7;

    [DataField, AutoNetworkedField]
    public TimeSpan JumpTime = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan CrashTime = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype> Emote = "XenoRoar";

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RMC14/Effects/meteorimpact.ogg");

    [DataField, AutoNetworkedField]
    public EntityWhitelist Structures = new();

    [DataField, AutoNetworkedField]
    public float Knockback = 2;

    [DataField, AutoNetworkedField]
    public float ShakeCameraRange = RMCMathExtensions.CircleAreaFromSquareAbilityRange(7);

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(60);

    //What it counts as for structure damages
    [DataField, AutoNetworkedField]
    public ProtoId<ExplosionPrototype> ExplosionType = "RMCOB";

    [DataField, AutoNetworkedField]
    public EntProtoId SmokeEffect = "CMExplosionEffectGrenade";
}
