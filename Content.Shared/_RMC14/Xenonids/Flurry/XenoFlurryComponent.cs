using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Flurry;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoFlurryComponent : Component
{
    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new();

    [DataField, AutoNetworkedField]
    public float Range = 2;

    [DataField, AutoNetworkedField]
    public int? MaxTargets = 4;

    [DataField, AutoNetworkedField]
    public int HealAmount = 30;

    [DataField, AutoNetworkedField]
    public TimeSpan HealDelay = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public int HealCharges = 1;

    [DataField, AutoNetworkedField]
    public TimeSpan EmoteDelay = TimeSpan.FromSeconds(6);

    [DataField, AutoNetworkedField]
    public SoundSpecifier SlashSound = new SoundCollectionSpecifier("AlienClaw");

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype> Emote = "XenoRoar";

    [DataField, AutoNetworkedField]
    public EntProtoId AttackEffect = "RMCEffectExtraSlash";

    [DataField, AutoNetworkedField]
    public EntProtoId TelegraphEffect = "RMCEffectXenoTelegraphRed";

    [DataField, AutoNetworkedField]
    public EntProtoId HealEffect = "RMCEffectHealFlurry";
}
