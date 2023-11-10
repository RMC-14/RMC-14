using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Acid;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoAcidSystem))]
public sealed partial class CorrodableComponent : Component
{
}
