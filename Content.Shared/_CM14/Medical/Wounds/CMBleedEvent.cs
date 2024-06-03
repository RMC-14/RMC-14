using Content.Shared.Damage;

namespace Content.Shared._CM14.Medical.Wounds;

[ByRefEvent]
public record struct CMBleedEvent(DamageChangedEvent Damage, bool Handled = false);
