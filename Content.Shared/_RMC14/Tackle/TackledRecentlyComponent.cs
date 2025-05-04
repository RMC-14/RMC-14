using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Tackle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(TackleSystem))]
public sealed partial class TackledRecentlyComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, TackleTracker> Trackers = new();

    [DataField, AutoNetworkedField]
    public TimeSpan ExpireAfter = TimeSpan.FromSeconds(4);
}
