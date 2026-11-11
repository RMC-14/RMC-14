using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.IffTag;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCXenoIffTagSystem))]
public sealed partial class RMCXenoIffTagItemComponent : Component;
