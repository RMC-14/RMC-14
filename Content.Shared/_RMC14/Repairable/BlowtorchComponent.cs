using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Repairable;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCRepairableSystem))]
public sealed partial class BlowtorchComponent : Component;
