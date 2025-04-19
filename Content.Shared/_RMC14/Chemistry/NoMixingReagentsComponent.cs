using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedRMCChemistrySystem))]
public sealed partial class NoMixingReagentsComponent : Component;
