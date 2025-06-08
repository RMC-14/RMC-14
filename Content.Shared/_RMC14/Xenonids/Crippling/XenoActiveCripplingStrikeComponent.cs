using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Crippling;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoCripplingStrikeSystem))]
public sealed partial class XenoActiveCripplingStrikeComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan ExpireAt;

    [DataField, AutoNetworkedField]
    public bool NextSlashBuffed = true;

    [DataField, AutoNetworkedField]
    public TimeSpan SlowDuration = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public float DamageMult = 1.2f;

    [DataField, AutoNetworkedField]
    public LocId HitText = "cm-xeno-crippling-strike-hit";

    [DataField, AutoNetworkedField]
    public LocId? DeactivateText;

    [DataField, AutoNetworkedField]
    public LocId ExpireText = "cm-xeno-crippling-strike-expire";

    [DataField, AutoNetworkedField]
    public float? Speed;
}
