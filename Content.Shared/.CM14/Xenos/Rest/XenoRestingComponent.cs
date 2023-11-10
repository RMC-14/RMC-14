using Robust.Shared.GameStates;

namespace Content.Shared.CM14.Xenos.Rest;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoRestSystem))]
public sealed partial class XenoRestingComponent : Component
{
}
