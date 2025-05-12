using Content.Shared.Chat.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Charge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoChargeSystem))]
public sealed partial class XenoToggleChargingComponent : Component
{
    [DataField, AutoNetworkedField]
    public float MinimumSteps = 4;

    [DataField, AutoNetworkedField]
    public int MaxStage = 8;

    [DataField, AutoNetworkedField]
    public float StepIncrement = 1;

    [DataField, AutoNetworkedField]
    public float SpeedPerStage = 0.2f;

    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaPerStep = 3;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_footstep_charge1.ogg", AudioParams.Default.WithVolume(-4));

    [DataField, AutoNetworkedField]
    public int SoundEvery = 4;

    [DataField, AutoNetworkedField]
    public float MaxDeviation = 1;

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype>? Emote = "XenoRoar";

    [DataField, AutoNetworkedField]
    public TimeSpan? EmoteCooldown = TimeSpan.FromSeconds(20);

    [DataField, AutoNetworkedField]
    public TimeSpan LastMovedGrace = TimeSpan.FromSeconds(0.5);
}
