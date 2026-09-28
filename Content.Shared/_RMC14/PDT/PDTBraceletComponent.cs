using Robust.Shared.Audio;

namespace Content.Shared._RMC14.PDT;

[RegisterComponent]
public sealed partial class PDTBraceletComponent : Component
{
    [DataField]
    public EntityUid? LinkedLocator;

    [DataField]
    public string? Serial;

    [DataField]
    public SoundSpecifier PingSound = new SoundPathSpecifier("/Audio/Items/locator_beep.ogg", AudioParams.Default.WithMaxDistance(5f));
}
