using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedTacticalMapSystem))]
public sealed partial class TacticalMapLiveUpdateOnOviComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;
}
