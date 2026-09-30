namespace Content.Shared._RMC14.FaxResponder;

[ByRefEvent]
public record struct FaxResponderStatusChangedEvent(int WeyaSlots, int FreePressSlots);
