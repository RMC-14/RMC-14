using Content.Shared.Damage;

namespace Content.Shared._RMC14.Medical.Wounds;

[ByRefEvent]
public record struct CMBleedEvent(DamageChangedEvent Damage, bool Handled = false);
