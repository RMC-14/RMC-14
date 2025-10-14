using Content.Shared.Damage;
using Content.Shared.Physics;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Leap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoLeapSystem))]
public sealed partial class XenoLeapingComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityCoordinates Origin;

    [DataField, AutoNetworkedField]
    public TimeSpan ParalyzeTime;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? LeapSound;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LeapEndTime;

    [DataField, AutoNetworkedField]
    public TimeSpan MoveDelayTime;

    [DataField, AutoNetworkedField]
    public bool KnockedDown;

    [DataField, AutoNetworkedField]
    public bool PlayedSound;

    [DataField, AutoNetworkedField]
    public bool KnockdownRequiresInvisibility;

    [DataField, AutoNetworkedField]
    public bool DestroyObjects;

    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new ();

    [DataField, AutoNetworkedField]
    public EntProtoId? HitEffect;

    [DataField, AutoNetworkedField]
    public TimeSpan TargetJitterTime;

    [DataField, AutoNetworkedField]
    public int TargetCameraShakeStrength;

    [DataField, AutoNetworkedField]
    public CollisionGroup IgnoredCollisionGroupLarge;

    [DataField, AutoNetworkedField]
    public CollisionGroup IgnoredCollisionGroupSmall;
}
