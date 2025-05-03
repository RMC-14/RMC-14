using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Headbite;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoHeadbiteSystem))]
public sealed partial class XenoHeadbiteComponent : Component
{
    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new();

    [DataField, AutoNetworkedField]
    public TimeSpan HeadbiteDelay = TimeSpan.FromSeconds(0.8);

    [DataField, AutoNetworkedField]
    public TimeSpan HealDelay = TimeSpan.FromSeconds(0.05);

    [DataField, AutoNetworkedField]
    public SoundSpecifier HitSound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_bite2.ogg");

    [DataField, AutoNetworkedField]
    public EntProtoId HealEffect = "RMCEffectHealHeadbite";

    [DataField, AutoNetworkedField]
    public int HealAmount = 150;

    [DataField, AutoNetworkedField]
    public EntProtoId HeadbiteEffect = "RMCEffectHeadbite";

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype> Emote = "XenoRoar";

    [DataField, AutoNetworkedField]
    public TimeSpan? EmoteCooldown = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public TimeSpan JitterTime = TimeSpan.FromSeconds(3);
}
