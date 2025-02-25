using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Intel;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(IntelSystem))]
public sealed partial class IntelUnlocksComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntityUid> Unlocks = new();
}
