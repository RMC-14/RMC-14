using Content.Shared._CM14.Medical.Surgery;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._CM14.Xenonids.Parasite;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedXenoParasiteSystem))]
public sealed partial class VictimInfectedComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ContainerId = "cm_parasite_container";

    [DataField, AutoNetworkedField]
    public SpriteSpecifier InfectedSprite = new Rsi(new ResPath("/Textures/_CM14/Mobs/Xenonids/Parasite/parasite_mask.rsi"), "human");

    [DataField, AutoNetworkedField]
    public SpriteSpecifier[] InfectedIcons =
    [
        new Rsi(new ResPath("/Textures/_CM14/Interface/xeno_hud.rsi"), "infected0"),
        new Rsi(new ResPath("/Textures/_CM14/Interface/xeno_hud.rsi"), "infected1"),
        new Rsi(new ResPath("/Textures/_CM14/Interface/xeno_hud.rsi"), "infected2"),
        new Rsi(new ResPath("/Textures/_CM14/Interface/xeno_hud.rsi"), "infected3"),
        new Rsi(new ResPath("/Textures/_CM14/Interface/xeno_hud.rsi"), "infected4"),
        new Rsi(new ResPath("/Textures/_CM14/Interface/xeno_hud.rsi"), "infected5"),
        new Rsi(new ResPath("/Textures/_CM14/Interface/xeno_hud.rsi"), "infected6")
    ];

    [DataField, AutoNetworkedField]
    public Enum InfectedLayer = VictimInfectedLayer.Infected;

    [DataField, AutoNetworkedField]
    public TimeSpan FallOffDelay = TimeSpan.FromSeconds(10);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan FallOffAt;

    [DataField, AutoNetworkedField]
    public bool FellOff;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan RecoverAt;

    [DataField, AutoNetworkedField]
    public bool Recovered;

    [DataField, AutoNetworkedField]
    public TimeSpan BurstDelay = TimeSpan.FromMinutes(8);

    [DataField, AutoNetworkedField]
    public TimeSpan AttachedAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan BurstAt;

    [DataField, AutoNetworkedField]
    public float IncubationMultiplier = 1;

    [DataField, AutoNetworkedField]
    public EntProtoId BurstSpawn = "CMXenoLarva";

    [DataField, AutoNetworkedField]
    public SoundSpecifier BurstSound = new SoundCollectionSpecifier("XenoChestBurst");

    [DataField, AutoNetworkedField, Access(typeof(SharedCMSurgerySystem))]
    public bool RootsCut;

    [DataField, AutoNetworkedField]
    public EntityUid? Hive;
}
