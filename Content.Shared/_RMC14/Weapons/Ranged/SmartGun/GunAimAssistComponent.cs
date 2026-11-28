using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Weapons.Ranged.SmartGun;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(GunAimAssistSystem))]
public sealed partial class GunAimAssistComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active;

    [DataField, AutoNetworkedField]
    public float LockRange = 2;

    [DataField, AutoNetworkedField]
    public FixedPoint2 AccuracyPenalty = 0.25;

    [DataField, AutoNetworkedField]
    public float BatteryDrain = 14.64f;

    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "RMCActionToggleAimAssist";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public SoundSpecifier ToggleSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/click.ogg");

    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan TargetExpires;

    [DataField, AutoNetworkedField]
    public TimeSpan TargetDuration = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi TargetIcon = new(new ResPath("_RMC14/Effects/smartgun_lock.rsi"), "lock");

    [DataField, AutoNetworkedField]
    public Color TargetColor = Color.White.WithAlpha(190 / 255f);

    [DataField]
    public bool Retargeted;
}
