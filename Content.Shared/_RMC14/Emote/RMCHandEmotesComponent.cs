using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Emote;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedRMCEmoteSystem))]
public sealed partial class RMCHandEmotesComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active = false;

    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    [DataField, AutoNetworkedField]
    public EntityUid? SpawnedEffect;

    [DataField, AutoNetworkedField]
    public RMCHandsEmoteState State = RMCHandsEmoteState.Fistbump;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? TailSwipeSound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_claw_block.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.1f),
    };

    [DataField, AutoNetworkedField]
    public EntProtoId TailSwipeEffect = "RMCEffectTailswipe";

    [DataField, AutoNetworkedField]
    public SoundSpecifier? FistBumpSound = new SoundPathSpecifier("/Audio/_RMC14/Entrenching/thud.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.5f),
    };

    [DataField, AutoNetworkedField]
    public EntProtoId FistBumpEffect = "RMCEffectFistbump";

    [DataField, AutoNetworkedField]
    public SoundSpecifier? HighFiveSound = new SoundPathSpecifier("/Audio/Items/snap.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.1f),
    };

    [DataField, AutoNetworkedField]
    public EntProtoId HighFiveEffect = "RMCEffectHighfive";

    [DataField, AutoNetworkedField]
    public SoundSpecifier? HugSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.5f),
    };

    [DataField, AutoNetworkedField]
    public EntProtoId HugEffect = "RMCEffectHug";

    [DataField, AutoNetworkedField]
    public TimeSpan LeftHangingDelay = TimeSpan.FromSeconds(10);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LeaveHangingAt;
}

[Serializable, NetSerializable]
public enum RMCHandsEmoteState : byte
{
    Fistbump = 0,
    Highfive = 1,
    Tailswipe = 2,
    Hug = 3
}
