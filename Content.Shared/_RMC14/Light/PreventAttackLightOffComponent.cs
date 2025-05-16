using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Light;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CMPoweredLightSystem))]
public sealed partial class PreventAttackLightOffComponent : Component;
