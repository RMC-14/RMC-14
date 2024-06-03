using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoSystem))]
public sealed partial class XenoFriendlyComponent : Component;
