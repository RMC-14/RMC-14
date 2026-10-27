using Robust.Shared.Audio;

namespace Content.Shared._RMC14.AntiAir;

[RegisterComponent]
[Access(typeof(RMCShipAntiAirSystem))]
public sealed partial class RMCShipAntiAirDropshipComponent : Component
{
    public string OriginalZone = string.Empty;

    public string DivertedZone = string.Empty;

    public TimeSpan AnnounceAt;

    public bool Announced;

    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RMC14/Effects/antiair_explosions.ogg");

    public int ShakeIntensity;

    public int ShakeDuration;
}
