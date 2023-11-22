using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Fortify;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoFortifySystem))]
public sealed partial class XenoFortifyActionComponent : Component
{
}
