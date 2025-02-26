using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Intel;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(IntelSystem))]
public sealed partial class IntelHasUnlockedComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<int> Unlocked = new();
}
