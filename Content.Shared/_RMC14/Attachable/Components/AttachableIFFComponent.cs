using Content.Shared._RMC14.Attachable.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Attachable.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(AttachableIFFSystem))]
public sealed partial class AttachableIFFComponent : Component;
