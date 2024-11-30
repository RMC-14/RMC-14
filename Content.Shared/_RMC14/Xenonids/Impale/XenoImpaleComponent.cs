using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Impale;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoImpaleSystem))]
public sealed partial class XenoImpaleComponent : Component
{
    [DataField, AutoNetworkedField]
    public int PlasmaCost = 80;

    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new();

    [DataField, AutoNetworkedField]
    public EntProtoId Animation = "RMCEffectTailHit";

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_tail_attack.ogg");

    [DataField, AutoNetworkedField]
    public int AP = 10;

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype>? Emote = "XenoRoar";

    [DataField, AutoNetworkedField]
    public TimeSpan? EmoteCooldown = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public TimeSpan SecondImpaleTime = TimeSpan.FromSeconds(0.4);
}
