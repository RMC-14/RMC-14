using Content.Shared._RMC14.Announce;
using Robust.Shared.GameObjects;
using System.Numerics;

namespace Content.Client._RMC14.Announce;

public sealed class AnnouncementDisplayData
{
    public string AnnouncementId { get; set; } = string.Empty;
    public string[] Text { get; set; } = Array.Empty<string>();
    public float Priority { get; set; }
    public bool CanInterrupt { get; set; }
    public bool CanBeInterrupted { get; set; }
    public AnnouncementPresentation Presentation { get; set; } = new();
    public NetEntity? SpeakerEntity { get; set; }
    public string? SpeakerName { get; set; }

    public AnnouncementStyle Style => Presentation.Style;
    public bool ShowSprite => Presentation.ShowSprite;
    public string? DecalRsi => Presentation.DecalRsi;
    public string? DecalState => Presentation.DecalState;
    public AnnouncementDecalPlacement? DecalPlacement => Presentation.DecalPlacement;
    public float DecalScale => Presentation.DecalScale;
    public float DecalAlpha => Presentation.DecalAlpha;
    public Vector2 DecalOffset => Presentation.DecalOffset;
    public Vector2 TextOffset => Presentation.TextOffset;
    public float SpriteScale => 1f;
    public string Title => Presentation.Style.TitleConfig.Title;
}
