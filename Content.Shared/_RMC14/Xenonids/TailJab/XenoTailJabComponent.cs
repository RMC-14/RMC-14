using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.TailJab;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoTailJabSystem))]
public sealed partial class XenoTailJabComponent : Component
{
    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage;

    [DataField, AutoNetworkedField]
    public EntProtoId AttackEffect = "RMCEffectExtraSlash";

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_tail_attack.ogg");

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype> Emote = "XenoRoar";

    [DataField, AutoNetworkedField]
    public TimeSpan? EmoteCooldown = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public TimeSpan SlowdownTime = TimeSpan.FromSeconds(0.5);

    [DataField, AutoNetworkedField]
    public TimeSpan WallSlamSlowdownTime = TimeSpan.FromSeconds(0.5);

    [DataField, AutoNetworkedField]
    public TimeSpan WallSlamStunTime = TimeSpan.FromSeconds(0.5);

    [DataField, AutoNetworkedField]
    public float ThrowRange = 0.25f;
}
