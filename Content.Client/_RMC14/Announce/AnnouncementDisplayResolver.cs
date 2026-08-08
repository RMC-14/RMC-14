using Content.Shared._RMC14.Announce;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Client._RMC14.Announce;

public static class AnnouncementDisplayResolver
{
    public static bool TryResolve(
        IPrototypeManager prototypeManager,
        ISerializationManager serialization,
        AnnouncementNetData data,
        AnnouncementDisplayPreference preference,
        out AnnouncementDisplayData resolved)
    {
        resolved = default!;

        if (!prototypeManager.TryIndex<AnnouncementPresetPrototype>(data.AnnouncementId, out var preset))
            return false;

        if (preference == AnnouncementDisplayPreference.Disabled)
            return false;

        var presentation = AnnouncementPresentationResolver.Resolve(serialization, preset, preference);
        resolved = new AnnouncementDisplayData
        {
            AnnouncementId = data.AnnouncementId,
            Text = data.Text,
            Priority = data.Priority,
            CanInterrupt = data.CanInterrupt,
            CanBeInterrupted = data.CanBeInterrupted,
            Presentation = presentation,
            SpeakerEntity = data.SpeakerEntity,
            SpeakerName = data.SpeakerName
        };
        return true;
    }
}
