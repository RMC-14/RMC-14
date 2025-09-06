using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.JoinXeno;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedJoinXenoSystem))]
public sealed partial class JoinXenoCooldownIgnoreComponent : Component;
