using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Announce;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class AnnouncementPresentation
{
    [DataField]
    private AnnouncementStyle style = new();

    [DataField]
    public bool ShowSprite { get; set; } = true;

    [DataField]
    public string? DecalRsi { get; set; }

    [DataField]
    public string? DecalState { get; set; }

    [DataField]
    public AnnouncementDecalPlacement? DecalPlacement { get; set; }

    [DataField]
    public float DecalScale { get; set; } = 4f;

    [DataField]
    public float DecalAlpha { get; set; } = 1f;

    [DataField]
    public Vector2 DecalOffset { get; set; } = Vector2.Zero;

    [DataField]
    public Vector2 TextOffset { get; set; } = Vector2.Zero;

    public AnnouncementStyle Style => style ??= new AnnouncementStyle();
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class AnnouncementPresentationSet
{
    [DataField(required: true)]
    public AnnouncementPresentation Stylized { get; set; } = new();

    [DataField]
    public AnnouncementPresentation? Default { get; set; }

    [DataField]
    public AnnouncementPresentation? Simplified { get; set; }

    public AnnouncementPresentation GetPresentation(AnnouncementDisplayPreference preference)
    {
        return preference switch
        {
            AnnouncementDisplayPreference.Default => Default ?? Stylized,
            AnnouncementDisplayPreference.Simplified => Simplified ?? Default ?? Stylized,
            _ => Stylized
        };
    }

    public IEnumerable<AnnouncementPresentation> EnumerateAvailable()
    {
        yield return Stylized;

        if (Default != null)
            yield return Default;

        if (Simplified != null)
            yield return Simplified;
    }
}
