using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Construction;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CMConstructionSystem))]
public sealed partial class DisableConstructionComponent : Component;
