using Robust.Shared.GameStates;

namespace Content.Shared.CM14.Xenos.Acid;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoAcidSystem))]
public sealed partial class CorrodableComponent : Component
{
}
