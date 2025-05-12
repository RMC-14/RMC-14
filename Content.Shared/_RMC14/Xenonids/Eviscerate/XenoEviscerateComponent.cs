using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Eviscerate;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoEviscerateComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public List<DamageSpecifier> DamageAtRageLevels;

    [DataField(required: true), AutoNetworkedField]
    public List<float> RangeAtRageLevels;

    [DataField(required: true), AutoNetworkedField]
    public List<TimeSpan> WindupReductionAtRageLevels;

    [DataField, AutoNetworkedField]
    public int LifeStealPerMarine = 50;

    [DataField, AutoNetworkedField]
    public int MaxLifeSteal = 250;

    [DataField, AutoNetworkedField]
    public TimeSpan WindupTime = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(1.25);

    [DataField, AutoNetworkedField]
    public TimeSpan HealDelay = TimeSpan.FromSeconds(0.05);

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("XenoTailSwipe");

    [DataField, AutoNetworkedField]
    public SoundSpecifier HitSound = new SoundCollectionSpecifier("RCMXenoClaw");

    [DataField, AutoNetworkedField]
    public SoundSpecifier RageHitSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/gibbed.ogg");

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype> RoarEmote = "XenoRoar";
}
