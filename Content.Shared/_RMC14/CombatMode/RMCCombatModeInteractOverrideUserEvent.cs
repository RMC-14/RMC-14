namespace Content.Shared._RMC14.CombatMode;

[ByRefEvent]
public record struct RMCCombatModeInteractOverrideUserEvent(EntityUid? Target, bool CanInteract = true, bool Handled = false);
