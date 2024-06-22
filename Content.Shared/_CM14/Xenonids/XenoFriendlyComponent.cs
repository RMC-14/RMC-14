using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenonids;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoSystem))]
public sealed partial class XenoFriendlyComponent : Component;
