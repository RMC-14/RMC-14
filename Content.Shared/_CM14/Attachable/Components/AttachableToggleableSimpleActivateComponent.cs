using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Attachable.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedAttachableToggleableSystem))]
public sealed partial class AttachableToggleableSimpleActivateComponent : Component;
