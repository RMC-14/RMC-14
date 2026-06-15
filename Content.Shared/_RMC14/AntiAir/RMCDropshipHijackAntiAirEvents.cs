using Robust.Shared.Audio;

namespace Content.Shared._RMC14.AntiAir;

[ByRefEvent]
public record struct RMCGetHijackDestinationEvent
{
    public RMCGetHijackDestinationEvent(EntityUid dropship, EntityUid originalDestination, EntityUid actor)
    {
        Dropship = dropship;
        OriginalDestination = originalDestination;
        Destination = originalDestination;
        Actor = actor;
    }

    public EntityUid Dropship;
    public EntityUid OriginalDestination;
    public EntityUid Destination;
    public EntityUid Actor;
    public EntityUid? AntiAirConsole;
    public string? OriginalZone;
    public string? DivertedZone;
    public bool Deterrence;
    public SoundSpecifier? DeterrenceSound;
    public int DeterrenceShakeIntensity;
    public int DeterrenceShakeDuration;
}

[ByRefEvent]
public readonly record struct RMCDropshipHijackAntiAirResolvedEvent(
    EntityUid Dropship,
    EntityUid Actor,
    EntityUid OriginalDestination,
    EntityUid Destination,
    EntityUid? AntiAirConsole,
    string? OriginalZone,
    string? DivertedZone,
    bool Deterrence,
    SoundSpecifier? DeterrenceSound,
    int DeterrenceShakeIntensity,
    int DeterrenceShakeDuration);
