using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Marines.Announce;

public sealed class SignedAnnouncementOptions
{
    public SoundSpecifier? Sound { get; init; }
    public Filter? Filter { get; init; }
    public bool ExcludeSurvivors { get; init; } = true;
    public bool SendOverlay { get; init; } = true;
}
