using Content.Shared._RMC14.Announce;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;

namespace Content.Server._RMC14.Announce;

public sealed partial class GeneralAnnounceSystem
{
    private const string PresetMarineCommand = "MarineCommand";
    private const string PresetAres = "Ares";
    private const string PresetCritical = "Critical";
    private const string PresetCLF = "CLF";

    public void AnnounceAsPlayer(
        EntityUid playerEntity,
        string message,
        string? presetId = null,
        AnnouncementTarget target = AnnouncementTarget.All,
        string? roleOverride = null)
    {
        var request = new AnnouncementRequest
        {
            Message = message,
            Preset = presetId ?? PresetMarineCommand,
            Target = target,
            Speaker = playerEntity,
            SpeakerNameOverride = roleOverride
        };

        AnnounceAdvanced(request);
    }

    public void AnnounceHighCommand(string message, string? author = null, SoundSpecifier? sound = null)
    {
        var wrappedMessage = author != null
            ? $"{author}: {message}"
            : message;

        var request = new AnnouncementRequest
        {
            Message = wrappedMessage,
            Preset = PresetMarineCommand,
            Target = AnnouncementTarget.Marines,
            SoundOverride = sound
        };

        AnnounceAdvanced(request);
    }

    public void AnnounceARES(EntityUid? source, string message, SoundSpecifier? sound = null)
    {
        var request = new AnnouncementRequest
        {
            Message = message,
            Preset = PresetAres,
            Target = AnnouncementTarget.All,
            Source = source,
            SpeakerNameOverride = "A.R.E.S.",
            SoundOverride = sound
        };

        AnnounceAdvanced(request);
    }

    public void AnnounceCritical(string message)
    {
        var request = new AnnouncementRequest
        {
            Message = message,
            Preset = PresetCritical,
            Target = AnnouncementTarget.All
        };

        AnnounceAdvanced(request);
    }

    public void AnnounceCLF(EntityUid? source, string message, SoundSpecifier? sound = null)
    {
        var request = new AnnouncementRequest
        {
            Message = message,
            Preset = PresetCLF,
            Target = AnnouncementTarget.All,
            Source = source,
            ShowSprite = false,
            SoundOverride = sound
        };

        AnnounceAdvanced(request);
    }
}
