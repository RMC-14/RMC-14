using Content.Shared._RMC14.Announce.Animations;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Announce;

[RegisterComponent]
[Access(typeof(RMCAnnouncementOverlaySystem))]
public sealed partial class RMCAnnouncementPresetComponent : Component
{
    [DataField]
    public RMCAnnouncementTemplate Template = RMCAnnouncementTemplate.Banner;

    [DataField]
    public RMCAnnouncementPosition Position = RMCAnnouncementPosition.TopLeft;

    [DataField]
    public int Priority = 5;

    [DataField]
    public bool Interruptible = true;

    [DataField]
    public bool Standalone;

    [DataField]
    public bool ShowSpeakerName;

    [DataField]
    public bool CenterText;

    [DataField]
    public LocId? Title;

    [DataField]
    public Color? TextColor;

    [DataField]
    public Color AccentColor = Color.White;

    [DataField]
    public Color? BackgroundColor;

    [DataField]
    public string Font = "Cozette";

    [DataField]
    public string TitleFont = "CozetteBold";

    [DataField]
    public float Scale = 1;

    [DataField]
    public int FontSize = 16;

    [DataField]
    public int TitleFontSize = 16;

    [DataField]
    public int TextScaleReference = 80;

    [DataField]
    public float MinTextScale = 0.5f;

    [DataField]
    public float MaxWidth = 520;

    [DataField]
    public SpriteSpecifier? Decal;

    [DataField]
    public float DecalScale = 4;

    [DataField]
    public string? Crt;

    [DataField]
    public string? CrtPortrait;

    [DataField]
    public string? BackgroundShader;

    [DataField]
    public Color? ShadowColor;

    [DataField]
    public TimeSpan PulseDuration;

    [DataField]
    public bool PulseForever;

    [DataField]
    public float PulseSpeed = 2;

    [DataField]
    public float PulseStrength = 0.6f;

    [DataField]
    public TimeSpan PowerOn = TimeSpan.FromSeconds(0.35);

    [DataField]
    public string? PortraitShader;

    [DataField]
    public Color PortraitBackgroundColor = Color.Black;

    [DataField]
    public IAnnouncementAnimationConfig Animation = new TypewriterAnimationConfig();

    [DataField]
    public TimeSpan MaxRevealTime = TimeSpan.FromSeconds(6);

    [DataField]
    public TimeSpan TitleDecrypt = TimeSpan.FromSeconds(0.5);

    [DataField]
    public bool Cursor = true;

    [DataField]
    public SoundSpecifier? TypingSound;

    [DataField]
    public int TypingSoundInterval = 3;

    [DataField]
    public TimeSpan Hold = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan? TotalTime;

    [DataField]
    public TimeSpan FadeOut = TimeSpan.FromSeconds(0.5);

    [DataField]
    public int MaxLines = 10;

    [DataField]
    public int MaxLength = 1000;

    [DataField]
    public EntProtoId<RMCAnnouncementPresetComponent>? SettingsGroup;

    [DataField]
    public bool ShowInLayoutEditor = true;
}
