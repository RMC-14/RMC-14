using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenonids.Crest;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoCrestSystem))]
public sealed partial class XenoCrestComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Lowered;

    [DataField, AutoNetworkedField]
    public int Armor = 5;

    [DataField, AutoNetworkedField]
    public float SpeedMultiplier = 0.70f;

    [DataField, AutoNetworkedField]
    public string ImmuneToStatus = "Stun";
}
