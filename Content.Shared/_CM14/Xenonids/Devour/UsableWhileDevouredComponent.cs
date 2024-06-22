using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenonids.Devour;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoDevourSystem))]
public sealed partial class UsableWhileDevouredComponent : Component;
