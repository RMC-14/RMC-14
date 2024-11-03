using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Ladder;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedLadderSystem))]
public sealed partial class LadderWatchingComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Watching;
}
