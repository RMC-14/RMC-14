using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Damage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCXenoDamageVisualsSystem))]
public sealed partial class RMCXenoDamageVisualsComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string Prefix;

    [DataField, AutoNetworkedField]
    public int States = 3;
}

[Serializable, NetSerializable]
public enum RMCDamageVisuals
{
    State,
}


[Serializable, NetSerializable]
public enum RMCDamageVisualLayers
{
    Base,
}
