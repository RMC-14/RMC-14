using Content.Shared._RMC14.Line;
using Content.Shared._RMC14.Stun;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Retrieve;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoRetrieveSystem))]
public sealed partial class XenoRetrieveComponent : Component
{
    [DataField, AutoNetworkedField]
    public RMCSizes SizeLimit = RMCSizes.Big;

    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(0.6);

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public float Range = 10;

    [DataField, AutoNetworkedField]
    public float Force = 15;

    // TODO RMC14 bang.ogg
    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_footstep_charge1.ogg");

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype>? Emote = "XenoRoar";

    [DataField, AutoNetworkedField]
    public EntProtoId Visual = "RMCEffectXenoTelegraphGreen";

    [DataField, AutoNetworkedField]
    public List<EntityUid> Visuals = new();
}
