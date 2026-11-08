using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
namespace Content.Shared._RMC14.Xenonids.AdrenalineSurge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoAdrenalineSurgeSystem))]
public sealed partial class XenoAdrenalineSurgeComponent : Component
{
    [DataField, AutoNetworkedField]
    public float SpeedModifierAmount = 1.65f;

    [DataField, AutoNetworkedField]
    public TimeSpan SurgeDuration = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(20);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? SurgeEndTime;

    [DataField, AutoNetworkedField]
    public bool IsSurging;

    [DataField, AutoNetworkedField]
    public bool IsUsable = true;
}
