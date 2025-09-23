using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Interaction;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCInteractionSystem))]
public sealed partial class NoHandsInteractionBlockedComponent : Component;
