using Robust.Shared.GameStates;

namespace Content.Shared.CM14.Marines;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedMarineSystem))]
public sealed partial class MarineComponent : Component
{
}
