using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Disposal;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CMDisposalSystem))]
public sealed partial class UndisposableComponent : Component;
