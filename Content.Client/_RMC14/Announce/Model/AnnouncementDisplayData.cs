using System.Numerics;
using Content.Shared._RMC14.Announce;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Announce;

public sealed class AnnouncementDisplayData
{
    public ProtoId<AnnouncementPresetPrototype> AnnouncementId { get; set; } = string.Empty;
    public string[] Text { get; set; } = Array.Empty<string>();
    public float Priority { get; set; }
    public bool CanInterrupt { get; set; }
    public bool CanBeInterrupted { get; set; }
    public AnnouncementPresentation Presentation { get; set; } = new();
    public NetEntity? SpeakerEntity { get; set; }
    public string? SpeakerName { get; set; }
    public Vector2? ScreenPositionOverride { get; set; }
    public float LayoutScale { get; set; } = 1f;
    public bool? ShowTitleOverride { get; set; }
    public bool? ShowSpriteOverride { get; set; }
    public Color? TextColorOverride { get; set; }
    public Color? TitleColorOverride { get; set; }
    public float? BodyTextScaleOverride { get; set; }
    public float? TitleTextScaleOverride { get; set; }
    public float VisualScale => LayoutScale;

    public AnnouncementStyle Style => Presentation.Style;
    public bool SupportsSpriteCardOverride => Presentation.ShowSprite && Presentation.Style.SpriteConfig.ShowSpriteBox;
    public bool ShowSprite => SupportsSpriteCardOverride
        ? ShowSpriteOverride ?? Presentation.ShowSprite
        : Presentation.ShowSprite;
    public string? DecalRsi => Presentation.DecalRsi;
    public string? DecalState => Presentation.DecalState;
    public AnnouncementDecalPlacement? DecalPlacement => Presentation.DecalPlacement;
    public float DecalScale => Presentation.DecalScale * LayoutScale;
    public float DecalAlpha => Presentation.DecalAlpha;
    public Vector2 DecalOffset => Presentation.DecalOffset * LayoutScale;
    public Vector2 TextOffset => Presentation.TextOffset * LayoutScale;
    public float SpriteScale => 1f;
    public string Title => Presentation.Style.TitleConfig.Title;
}
