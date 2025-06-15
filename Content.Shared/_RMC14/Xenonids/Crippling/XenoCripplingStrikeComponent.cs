using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Crippling;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoCripplingStrikeSystem))]
public sealed partial class XenoCripplingStrikeComponent : Component
{
    [DataField, AutoNetworkedField]
    public float DamageMult = 1.2f;

    [DataField, AutoNetworkedField]
    public TimeSpan ActiveDuration = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public TimeSpan SlowDuration = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public Color? AuraColor;

    [DataField, AutoNetworkedField]
    public LocId ActivateText = "cm-xeno-crippling-strike-activate";

    [DataField, AutoNetworkedField]
    public LocId HitText = "cm-xeno-crippling-strike-hit";

    [DataField, AutoNetworkedField]
    public LocId? DeactivateText;

    [DataField, AutoNetworkedField]
    public LocId ExpireText = "cm-xeno-crippling-strike-expire";

    [DataField, AutoNetworkedField]
    public float? Speed;
}
