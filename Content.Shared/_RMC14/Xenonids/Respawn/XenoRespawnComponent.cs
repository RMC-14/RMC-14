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
    public bool RespawnAtLocation = false;

    [DataField]
    public EntityCoordinates? Location;

    [DataField]
    public EntProtoId Larva = "CMXenoLarva";

    /// <summary>
    ///     How long the respawned larva is invincible for when it spawns at the corpse (the fallback when no hive core
    ///     is available). Respawns that go through the hive instead use the per-location handles on <see cref="HiveComponent"/>.
    ///     Set to zero or less to disable.
    /// </summary>
    [DataField]
    public TimeSpan CorpseInvincibilityTime = TimeSpan.FromSeconds(5);

    [DataField]
    public SoundSpecifier CorpseSound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/xeno_newlarva.ogg");
}
