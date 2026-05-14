using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared._RMC14.Announce;

public static class AnnouncementPresentationResolver
{
    public static AnnouncementPresentation Resolve(
        AnnouncementPresetPrototype preset,
        AnnouncementDisplayPreference preference)
    {
        var serialization = IoCManager.Resolve<ISerializationManager>();
        return serialization.CreateCopy(preset.Presentations.GetPresentation(preference), notNullableOverride: true)!;
    }
}
