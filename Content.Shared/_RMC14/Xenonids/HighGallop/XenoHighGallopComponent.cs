using Content.Shared.Chat.Prototypes;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.HighGallop;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoHighGallopComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId TelegraphEffect = "RMCEffectXenoTelegraphRed";

    [DataField, AutoNetworkedField]
    public EntProtoId TelegraphEffectEdge = "RMCEffectXenoTelegraphRedSmall";

    [DataField, AutoNetworkedField]
    public float Width = 3;

    [DataField, AutoNetworkedField]
    public float Height = 2;

    [DataField, AutoNetworkedField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan SlowDuration = TimeSpan.FromSeconds(2.5);

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_footstep_charge3.ogg");

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype> Emote = "XenoRoar";

    [DataField, AutoNetworkedField]
    public ProtoId<TagPrototype> Flingable = "Grenade";

    [DataField, AutoNetworkedField]
    public float FlingDistance = 3;
}
