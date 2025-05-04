using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Shared._RMC14.Xenonids.Respawn;

[RegisterComponent, NetworkedComponent]
public sealed partial class XenoRespawnComponent : Component
{
    [DataField]
    public EntityUid? Hive;

    [DataField]
    public TimeSpan RespawnAt;

    [DataField]
    public bool RespawnAtCorpse = false;

    [DataField]
    public EntityCoordinates? CorpseLocation;

    [DataField]
    public EntProtoId Larva = "CMXenoLarva";

    [DataField]
    public SoundSpecifier CorpseSound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/xeno_newlarva.ogg");
}
