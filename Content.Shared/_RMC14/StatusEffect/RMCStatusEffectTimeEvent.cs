namespace Content.Shared._RMC14.StatusEffect;

[ByRefEvent]
public record struct RMCStatusEffectTimeEvent(string Key, TimeSpan Duration);
