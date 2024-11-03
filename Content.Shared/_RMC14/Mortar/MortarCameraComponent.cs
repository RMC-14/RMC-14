using Content.Shared._RMC14.Camera;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Mortar;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedRMCCameraSystem))]
public sealed partial class MortarCameraComponent : Component;
