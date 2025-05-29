using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Cassette;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedCassetteSystem))]
public sealed partial class CassettePlayerComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId PlayPauseActionId = "RMCActionCassettePlayPause";

    [DataField, AutoNetworkedField]
    public EntityUid? PlayPauseAction;

    [DataField, AutoNetworkedField]
    public EntProtoId NextActionId = "RMCActionCassetteNext";

    [DataField, AutoNetworkedField]
    public EntityUid? NextAction;

    [DataField, AutoNetworkedField]
    public EntProtoId RestartActionId = "RMCActionCassetteRestart";

    [DataField, AutoNetworkedField]
    public EntityUid? RestartAction;

    [DataField, AutoNetworkedField]
    public SlotFlags Slots = SlotFlags.EARS;

    [DataField, AutoNetworkedField]
    public string ContainerId = "rmc_cassette_player";

    [DataField, AutoNetworkedField]
    public EntityUid? AudioStream;

    [DataField]
    public EntityUid? CustomAudioStream;

    [DataField, AutoNetworkedField]
    public AudioState State;

    [DataField, AutoNetworkedField]
    public AudioParams AudioParams = AudioParams.Default.WithVolume(-6f);

    [DataField, AutoNetworkedField]
    public int Tape;

    [DataField, AutoNetworkedField]
    public SoundSpecifier PlayPauseSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/click.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier InsertEjectSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/handcuffs.ogg");

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi WornSprite = new(new ResPath("_RMC14/Objects/Devices/cassette_player.rsi"), "mob_overlay");

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi MusicSprite = new(new ResPath("_RMC14/Objects/Devices/cassette_player.rsi"), "music");
}
