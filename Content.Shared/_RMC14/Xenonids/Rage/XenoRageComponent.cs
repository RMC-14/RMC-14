using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Rage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoRageSystem))]
public sealed partial class XenoRageComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Rage = 0;

    [DataField, AutoNetworkedField]
    public int MaxRage = 5;

    [DataField, AutoNetworkedField]
    public bool RageLocked = false;

    [DataField, AutoNetworkedField]
    public int ArmorPerRage = 3;

    [DataField, AutoNetworkedField]
    public float SpeedBuffPerRage = 0.05f;

    [DataField, AutoNetworkedField]
    public float AttackSpeedPerRage = 0.28f;

    [DataField, AutoNetworkedField]
    public TimeSpan RageDecayTime = TimeSpan.FromSeconds(0.5);

    [DataField, AutoNetworkedField]
    public TimeSpan RageLockDuration = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public TimeSpan RageCooldownDuration = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public TimeSpan RageHealTime = TimeSpan.FromSeconds(0.05);

    [DataField, AutoNetworkedField]
    public FixedPoint2 HealAmount = 45; // Equal to the slash damage of the xeno

    [DataField, AutoNetworkedField]
    public Color RageLockColor = Color.Black;

    [DataField, AutoNetworkedField]
    public Color RageLockWeakenColor = Color.White;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan RageLockExpireAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan RageCooldownExpireAt;
}
