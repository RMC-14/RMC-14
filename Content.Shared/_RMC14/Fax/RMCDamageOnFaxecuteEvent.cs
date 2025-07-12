using Content.Shared.Fax.Components;

namespace Content.Shared._RMC14.Fax;

/// <summary>
/// RMC-specific event for killing any mob within the fax machine.
/// This is RMC-specific functionality that extends the base fax system.
/// </summary>
[ByRefEvent]
public record struct RMCDamageOnFaxecuteEvent(FaxMachineComponent? Action);
