using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Construction.Nest;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoNestSystem))]
public sealed partial class XenoNestableComponent : Component;
