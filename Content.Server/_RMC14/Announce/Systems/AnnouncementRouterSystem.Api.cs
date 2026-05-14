using Content.Shared._RMC14.Announce;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Announce;

public sealed partial class AnnouncementRouterSystem
{
    private static readonly ProtoId<AnnouncementPresetPrototype> PresetMarineCommand = "MarineCommand";
    private static readonly ProtoId<AnnouncementPresetPrototype> PresetAres = "Ares";
    private static readonly ProtoId<AnnouncementPresetPrototype> PresetCritical = "Critical";
    private static readonly ProtoId<AnnouncementPresetPrototype> PresetCLF = "CLF";

    public void AnnounceAsPlayer(
        EntityUid playerEntity,
        string message,
        ProtoId<AnnouncementPresetPrototype>? presetId = null,
        AnnouncementTarget target = AnnouncementTarget.All,
        string? roleOverride = null)
    {
        var request = new AnnouncementRequest
        {
            Message = message,
            Preset = presetId ?? PresetMarineCommand,
            Route = new AnnouncementRoute
            {
                Target = target,
                Speaker = playerEntity,
                SpeakerNameOverride = roleOverride,
            }
        };

        Announce(request);
    }

    public void AnnounceHighCommand(string message, string? author = null, SoundSpecifier? sound = null)
    {
        var wrappedMessage = author != null
            ? $"{author}: {message}"
            : message;

        var request = new AnnouncementRequest
        {
            Message = message,
            Preset = PresetMarineCommand,
            Route = new AnnouncementRoute
            {
                Target = AnnouncementTarget.Marines,
                Channels = AnnouncementChannels.Chat | AnnouncementChannels.Overlay | AnnouncementChannels.Sound,
            },
            Chat = new AnnouncementChatOptions
            {
                Message = wrappedMessage,
                WrappedMessage = wrappedMessage,
            },
            Sound = new AnnouncementSoundOptions
            {
                Sound = sound,
            }
        };

        Announce(request);
    }

    public void AnnounceARES(EntityUid? source, string message, SoundSpecifier? sound = null)
    {
        var request = new AnnouncementRequest
        {
            Message = message,
            Preset = PresetAres,
            Route = new AnnouncementRoute
            {
                Target = AnnouncementTarget.All,
                Source = source,
                SpeakerNameOverride = "A.R.E.S.",
            },
            Sound = new AnnouncementSoundOptions
            {
                Sound = sound,
            }
        };

        Announce(request);
    }

    public void AnnounceCritical(string message)
    {
        var request = new AnnouncementRequest
        {
            Message = message,
            Preset = PresetCritical,
            Route = new AnnouncementRoute
            {
                Target = AnnouncementTarget.All,
            }
        };

        Announce(request);
    }

    public void AnnounceCLF(EntityUid? source, string message, SoundSpecifier? sound = null)
    {
        var request = new AnnouncementRequest
        {
            Message = message,
            Preset = PresetCLF,
            Route = new AnnouncementRoute
            {
                Target = AnnouncementTarget.All,
                Source = source,
            },
            Sound = new AnnouncementSoundOptions
            {
                Sound = sound,
            }
        };

        Announce(request);
    }
}
