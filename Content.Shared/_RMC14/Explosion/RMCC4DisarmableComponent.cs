using Content.Shared._RMC14.Xenonids.Acid;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Explosion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCC4DisarmableSystem))]
public sealed partial class RMCC4DisarmableComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan AcidDelay = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public XenoAcidStrength MinimumAcidStrength = XenoAcidStrength.Weak;

    [DataField, AutoNetworkedField]
    public TimeSpan MultitoolDelay = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public LocId AcidStartPopup = "rmc-c4-acid-start";

    [DataField, AutoNetworkedField]
    public LocId AcidFinishPopup = "rmc-c4-acid-finish";

    [DataField, AutoNetworkedField]
    public LocId AcidTooWeakPopup = "rmc-c4-acid-too-weak";

    [DataField, AutoNetworkedField]
    public LocId MultitoolStartPopup = "rmc-c4-disarm-start";

    [DataField, AutoNetworkedField]
    public LocId MultitoolStopPopup = "rmc-c4-disarm-stop";

    [DataField, AutoNetworkedField]
    public LocId MultitoolFinishPopup = "rmc-c4-disarm-finish";
}
