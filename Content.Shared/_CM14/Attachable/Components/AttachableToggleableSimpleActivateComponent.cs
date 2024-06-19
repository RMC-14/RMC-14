using Robust.Shared.GameStates;


namespace Content.Shared._CM14.Attachable;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedAttachableToggleableSystem))]
public sealed partial class AttachableToggleableSimpleActivateComponent : Component;
