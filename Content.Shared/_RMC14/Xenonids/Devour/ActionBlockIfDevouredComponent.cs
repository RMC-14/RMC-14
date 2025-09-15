using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Devour;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoDevourSystem))]
public sealed partial class ActionBlockIfDevouredComponent : Component;
