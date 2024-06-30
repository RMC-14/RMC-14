using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Attachable.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(AttachablePreventDropSystem))]
public sealed partial class AttachablePreventDropToggleableComponent : Component;
