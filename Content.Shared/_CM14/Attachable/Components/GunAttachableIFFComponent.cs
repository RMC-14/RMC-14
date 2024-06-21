using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Attachable.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(AttachableIFFSystem))]
public sealed partial class GunAttachableIFFComponent : Component;
