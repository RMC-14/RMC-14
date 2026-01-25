namespace Content.Shared._RMC14.Medical.HUD.Events;

/// <summary>
/// Relays a holocard change to a container.
/// </summary>
/// <param name="NewStatus"></param>
[ByRefEvent]
public record struct HolocardContainerStatusUpdateEvent(HolocardStatus NewStatus);
