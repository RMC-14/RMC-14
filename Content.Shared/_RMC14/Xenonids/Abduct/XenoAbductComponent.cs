using Content.Shared.Chat.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Abduct;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoAbductSystem))]
public sealed partial class XenoAbductComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan DoafterTime = TimeSpan.FromSeconds(0.8);

    [DataField, AutoNetworkedField]
    public EntProtoId Telegraph = "RMCEffectXenoTelegraphAbduct";

    [DataField]
    public List<EntityUid> Tiles = new();

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype>? Emote = "XenoRoar";

    [DataField, AutoNetworkedField]
    public FixedPoint2 Cost = 180;

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_footstep_charge1.ogg");

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(15);

    [DataField, AutoNetworkedField]
    public int Range = 6;

    [DataField, AutoNetworkedField]
    public TimeSpan SlowTime = TimeSpan.FromSeconds(2.5);

    [DataField, AutoNetworkedField]
    public TimeSpan RootTime = TimeSpan.FromSeconds(2.5);

    [DataField, AutoNetworkedField]
    public TimeSpan DazeTime = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(2.6);

    [DataField, AutoNetworkedField]
    public int MaxTargets = 10;

    [DataField, AutoNetworkedField]
    public float TileRadius = 0.4f;
}
