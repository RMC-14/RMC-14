using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Movement;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCConfusedSystem))]
public sealed partial class RMCConfusedStatusEffectComponent : Component;
