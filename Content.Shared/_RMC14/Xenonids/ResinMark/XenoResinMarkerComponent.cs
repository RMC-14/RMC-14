using System.Collections.Generic;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.ResinMark;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoResinMarkerComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Creator;

    [DataField, AutoNetworkedField]
    public EntityUid Hive;

    [DataField, AutoNetworkedField]
    public string PingType = string.Empty;

    [DataField, AutoNetworkedField]
    public EntityUid? LinkedPing;

    [DataField]
    public HashSet<EntityUid> Watchers = new();
}
