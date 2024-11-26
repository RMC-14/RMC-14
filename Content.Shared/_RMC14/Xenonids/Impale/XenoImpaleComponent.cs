using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Impale;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoImpaleSystem))]
public sealed partial class XenoImpaleComponent : Component
{
    [DataField]
    public int PlasmaCost = 80;

    [DataField]
    public DamageSpecifier Damage = new();

    [DataField]
    public EntProtoId Animation = "RMCEffectTailHit";

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_tail_attack.ogg");

    [DataField]
    public int AP = 10;

    [DataField]
    public ProtoId<EmotePrototype>? Emote = "XenoRoar";

    [DataField]
    public TimeSpan? EmoteCooldown = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan SecondImpaleTime = TimeSpan.FromSeconds(0.4);
}
