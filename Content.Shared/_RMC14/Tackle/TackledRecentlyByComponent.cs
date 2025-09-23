using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Tackle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(TackleSystem))]
public sealed partial class TackledRecentlyByComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Tacklers = new();
}
