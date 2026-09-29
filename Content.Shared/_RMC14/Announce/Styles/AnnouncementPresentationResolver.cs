using Robust.Shared.Serialization.Manager;

namespace Content.Shared._RMC14.Announce;

public static class AnnouncementPresentationResolver
{
    public static AnnouncementPresentation Resolve(
        ISerializationManager serialization,
        AnnouncementPresetPrototype preset,
        AnnouncementDisplayPreference preference)
    {
        return serialization.CreateCopy(preset.Presentations.GetPresentation(preference), notNullableOverride: true)!;
    }
}
