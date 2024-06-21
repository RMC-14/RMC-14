using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Attachable.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(AttachableToggleableSystem))]
public sealed partial class AttachableToggleableSimpleActivateComponent : Component;
