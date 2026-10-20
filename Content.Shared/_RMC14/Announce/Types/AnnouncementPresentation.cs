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
    public bool ShowSprite { get; private set; } = true;

    [DataField]
    public string? DecalRsi { get; private set; }

    [DataField]
    public string? DecalState { get; private set; }

    [DataField]
    public AnnouncementDecalPlacement? DecalPlacement { get; private set; }

    [DataField]
    public float DecalScale { get; private set; } = 4f;

    [DataField]
    public float DecalAlpha { get; private set; } = 1f;

    [DataField]
    public Vector2 DecalOffset { get; private set; } = Vector2.Zero;

    [DataField]
    public Vector2 TextOffset { get; private set; } = Vector2.Zero;

    public AnnouncementStyle Style => style ??= new AnnouncementStyle();
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class AnnouncementPresentationSet
{
    [DataField]
    public AnnouncementPresentation? Stylized { get; set; }

    [DataField]
    public AnnouncementPresentation? Default { get; set; }

    [DataField]
    public AnnouncementPresentation? Simplified { get; set; }

    public AnnouncementPresentation GetPresentation(AnnouncementDisplayPreference preference)
    {
        return preference switch
        {
            AnnouncementDisplayPreference.Stylized => Stylized ?? Default ?? new(),
            AnnouncementDisplayPreference.Default => Default ?? Stylized ?? new(),
            AnnouncementDisplayPreference.Simplified => Simplified ?? Default ?? Stylized ?? new(),
            _ => Stylized ?? Default ?? new()
        };
    }

    public IEnumerable<AnnouncementPresentation> EnumerateAvailable()
    {
        if (Stylized != null)
            yield return Stylized;

        if (Default != null)
            yield return Default;

        if (Simplified != null)
            yield return Simplified;
    }
}
