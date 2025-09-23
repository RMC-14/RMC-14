using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.ScissorCut;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoScissorCutComponent : Component
{
    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new();

    [DataField, AutoNetworkedField]
    public float Range = 4;

    [DataField, AutoNetworkedField]
    public TimeSpan SuperSlowDuration = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public SoundSpecifier SlashSound = new SoundCollectionSpecifier("AlienClaw");

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype> Emote = "XenoRoar";

    [DataField, AutoNetworkedField]
    public EntProtoId AttackEffect = "RMCEffectExtraSlash";

    [DataField, AutoNetworkedField]
    public EntProtoId TelegraphEffect = "RMCEffectXenoTelegraphRed";

    [DataField, AutoNetworkedField]
    public EntProtoId TelegraphEffectEdge = "RMCEffectXenoTelegraphRedSmall";
}
