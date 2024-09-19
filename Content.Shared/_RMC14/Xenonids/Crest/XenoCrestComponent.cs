using Content.Shared._RMC14.Stun;
using Robust.Shared.GameStates;


namespace Content.Shared._RMC14.Xenonids.Crest;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoCrestSystem))]
public sealed partial class XenoCrestComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Lowered;

    [DataField, AutoNetworkedField]
    public int Armor;

    [DataField, AutoNetworkedField]
    public float SpeedMultiplier;

    [DataField, AutoNetworkedField]
    public string ImmuneToStatus = "Stun";

    [DataField, AutoNetworkedField]
    public RMCSizes CrestSize = RMCSizes.Big;

    [DataField, AutoNetworkedField]
    public RMCSizes? OriginalSize;
}
