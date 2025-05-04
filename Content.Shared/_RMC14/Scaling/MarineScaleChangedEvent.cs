namespace Content.Shared._RMC14.Scaling;

[ByRefEvent]
public readonly record struct MarineScaleChangedEvent(double New, double Delta);
