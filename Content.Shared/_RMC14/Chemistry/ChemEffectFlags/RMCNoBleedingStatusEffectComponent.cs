using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.ChemEffectFlags;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCChemEffectFlagsSystem))]
public sealed partial class RMCNoBleedingStatusEffectComponent : Component;
