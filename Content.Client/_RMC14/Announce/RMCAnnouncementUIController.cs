using System.Numerics;
using Content.Client.Gameplay;
using Content.Shared._RMC14.Announce;
using Content.Shared._RMC14.Announce.Animations;
using Content.Shared._RMC14.CCVar;
using Robust.Client.Audio;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Announce;

public sealed class RMCAnnouncementUIController : UIController,
    IOnStateEntered<GameplayState>,
    IOnStateExited<GameplayState>,
    IOnSystemChanged<RMCAnnouncementOverlayUISystem>
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    [UISystemDependency] private readonly AudioSystem? _audio = default!;

    public const int MinVisible = 1;
    public const int MaxVisible = 4;
    private const int MaxQueued = 32;
    private const string GlitchCharacters = "#$%&*@!?/|<>{}";

    private readonly List<ActiveAnnouncement> _active = new();
    private readonly List<RMCAnnouncementDisplay> _queue = new();
    private bool _inGameplay;
    private long _nextOrder;
    private int _maxVisible = 2;
    private Dictionary<string, RMCAnnouncementOverride> _overrides = new();
    private RMCAnnouncementArea? _updatingArea;

    public override void Initialize()
    {
        base.Initialize();
        _cfg.OnValueChanged(RMCCVars.RMCAnnouncementMaxVisible, OnMaxVisibleChanged, true);
        _cfg.OnValueChanged(RMCCVars.RMCAnnouncementOverrides, OnOverridesChanged, true);
    }

    public void OnSystemLoaded(RMCAnnouncementOverlayUISystem system)
    {
        system.AnnouncementReceived += OnAnnouncementReceived;
    }

    public void OnSystemUnloaded(RMCAnnouncementOverlayUISystem system)
    {
        system.AnnouncementReceived -= OnAnnouncementReceived;
    }

    public void OnStateEntered(GameplayState state)
    {
        _inGameplay = true;
        FillSlots();
    }

    public void OnStateExited(GameplayState state)
    {
        for (var i = _active.Count - 1; i >= 0; i--)
        {
            RemoveActive(i);
        }

        _queue.Clear();
        _inGameplay = false;
    }

    public static Vector2 GetDefaultPivot(RMCAnnouncementPosition position)
    {
        return position switch
        {
            RMCAnnouncementPosition.TopLeft => new Vector2(0, 0),
            RMCAnnouncementPosition.TopCenter => new Vector2(0.5f, 0),
            RMCAnnouncementPosition.TopRight => new Vector2(1, 0),
            RMCAnnouncementPosition.MiddleLeft => new Vector2(0, 0.5f),
            RMCAnnouncementPosition.MiddleCenter => new Vector2(0.5f, 0.5f),
            RMCAnnouncementPosition.MiddleRight => new Vector2(1, 0.5f),
            RMCAnnouncementPosition.BottomLeft => new Vector2(0, 1),
            RMCAnnouncementPosition.BottomCenter => new Vector2(0.5f, 1),
            RMCAnnouncementPosition.BottomRight => new Vector2(1, 1),
            _ => Vector2.Zero,
        };
    }

    private void OnMaxVisibleChanged(int value)
    {
        _maxVisible = Math.Clamp(value, MinVisible, MaxVisible);
        while (CountStacked() > _maxVisible)
        {
            var lowest = FindLowestPriorityActive();
            if (lowest < 0)
                break;

            RemoveActive(lowest);
        }

        FillSlots();
    }

    private void OnOverridesChanged(string serialized)
    {
        _overrides = RMCAnnouncementOverrides.Parse(serialized);
        RefreshActive();
    }

    private void OnAnnouncementReceived(RMCAnnouncementOverlayMsg msg)
    {
        if (!_prototype.TryIndex(msg.Preset, out var prototype) ||
            !prototype.TryGetComponent(out RMCAnnouncementPresetComponent? preset, _compFactory))
        {
            return;
        }

        var settings = RMCAnnouncementOverrides.Resolve(_overrides, msg.Preset.Id, GetSettingsKey(msg.Preset, preset));
        if (settings.Show == false)
            return;

        var display = new RMCAnnouncementDisplay
        {
            PresetId = msg.Preset,
            Preset = preset,
            Text = msg.Text,
            Title = GetTitle(preset, msg.Title),
            Accent = msg.Accent ?? preset.AccentColor,
            SpeakerName = msg.SpeakerName,
            SpeakerJob = msg.SpeakerJob,
            Speaker = settings.ShowPortrait != false ? msg.Portrait : null,
            Order = _nextOrder++,
        };

        ApplySettings(display, settings, GetArea()?.ScreenScale ?? 1);

        if (GetArea() == null)
        {
            Enqueue(display);
            return;
        }

        if (preset.Standalone || CountStacked() < _maxVisible)
        {
            Show(display);
            return;
        }

        var replace = FindInterruptible(preset.Priority);
        if (replace >= 0)
        {
            RemoveActive(replace);
            Show(display);
            return;
        }

        Enqueue(display);
    }

    private void Show(RMCAnnouncementDisplay display)
    {
        if (GetArea() is not { } area)
        {
            Enqueue(display);
            return;
        }

        if (_entities.TryGetEntity(display.Speaker, out var portrait))
            display.Portrait = portrait;

        var view = CreateView(display);
        var active = new ActiveAnnouncement(display, view, area, _timing.RealTime, GetRevealTime(display));
        _active.Add(active);
        area.Add(view, display.Pivot, display.AvoidHud);
        UpdateActive(active, _timing.RealTime);
    }

    public static string? GetTitle(RMCAnnouncementPresetComponent preset, string? title)
    {
        return title ?? (preset.Title is { } titleId ? Loc.GetString(titleId) : null);
    }

    public static void ApplySettings(RMCAnnouncementDisplay display, RMCAnnouncementOverride settings, float screenScale)
    {
        display.Settings = settings;
        display.ScreenScale = screenScale;
        display.Scale = (settings.Scale ?? 1) * display.Preset.Scale * screenScale;
        display.Pivot = settings.Pivot ?? GetDefaultPivot(display.Preset.Position);
        display.AvoidHud = settings.Pivot == null;
    }

    public static Control CreateView(RMCAnnouncementDisplay display)
    {
        Control view = display.Preset.Template == RMCAnnouncementTemplate.TitleCard
            ? new RMCAnnouncementTitleCard()
            : new RMCAnnouncementCard();

        ((IRMCAnnouncementView) view).Setup(display);
        return view;
    }

    private void RemoveActive(int index)
    {
        var active = _active[index];
        _active.RemoveAt(index);
        active.Area.Remove(active.View);
    }

    private void RefreshActive()
    {
        for (var i = _active.Count - 1; i >= 0; i--)
        {
            var active = _active[i];
            var display = active.Display;
            var settings = RMCAnnouncementOverrides.Resolve(_overrides, display.PresetId.Id, GetSettingsKey(display.PresetId, display.Preset));
            if (settings.Show == false)
            {
                RemoveActive(i);
                continue;
            }

            var screenScale = active.Area.ScreenScale;
            if (settings == display.Settings && MathHelper.CloseTo(screenScale, display.ScreenScale))
                continue;

            ApplySettings(display, settings, screenScale);
            active.Area.Remove(active.View);
            ((IRMCAnnouncementView) active.View).Setup(display);
            ((IRMCAnnouncementView) active.View).SetBody(active.Shown, active.Fade, UsesCursor(display.Preset), active.CursorVisible);
            active.Area.Add(active.View, display.Pivot, display.AvoidHud);
        }
    }

    private void FillSlots()
    {
        if (GetArea() == null)
            return;

        while (_queue.Count > 0)
        {
            var next = FindNextQueued();
            var display = _queue[next];
            if (!display.Preset.Standalone && CountStacked() >= _maxVisible)
                break;

            _queue.RemoveAt(next);
            Show(display);
        }
    }

    private void Enqueue(RMCAnnouncementDisplay display)
    {
        if (_queue.Count >= MaxQueued)
        {
            var lowest = FindLowestQueued();
            if (!IsHigherPriority(display, _queue[lowest]))
                return;

            _queue.RemoveAt(lowest);
        }

        _queue.Add(display);
    }

    private int CountStacked()
    {
        var count = 0;
        foreach (var active in _active)
        {
            if (!active.Display.Preset.Standalone)
                count++;
        }

        return count;
    }

    private int FindInterruptible(int priority)
    {
        for (var i = 0; i < _active.Count; i++)
        {
            var preset = _active[i].Display.Preset;
            if (!preset.Standalone && preset.Interruptible && preset.Priority < priority)
                return i;
        }

        return -1;
    }

    private int FindLowestPriorityActive()
    {
        var index = -1;
        for (var i = 0; i < _active.Count; i++)
        {
            var display = _active[i].Display;
            if (display.Preset.Standalone)
                continue;

            if (index < 0 || !IsHigherPriority(display, _active[index].Display))
                index = i;
        }

        return index;
    }

    private int FindNextQueued()
    {
        var best = 0;
        for (var i = 1; i < _queue.Count; i++)
        {
            if (IsHigherPriority(_queue[i], _queue[best]))
                best = i;
        }

        return best;
    }

    private int FindLowestQueued()
    {
        var worst = 0;
        for (var i = 1; i < _queue.Count; i++)
        {
            if (IsHigherPriority(_queue[worst], _queue[i]))
                worst = i;
        }

        return worst;
    }

    private static bool IsHigherPriority(RMCAnnouncementDisplay a, RMCAnnouncementDisplay b)
    {
        if (a.Preset.Priority != b.Preset.Priority)
            return a.Preset.Priority > b.Preset.Priority;

        return a.Order < b.Order;
    }

    private static TimeSpan GetRevealTime(RMCAnnouncementDisplay display)
    {
        return RMCAnnouncementOverlaySystem.GetRevealTime(display.Preset, display.Text.Length);
    }

    private void OnAreaFrameUpdate(FrameEventArgs args)
    {
        var time = _timing.RealTime;
        var finished = false;
        for (var i = _active.Count - 1; i >= 0; i--)
        {
            if (UpdateActive(_active[i], time))
                continue;

            RemoveActive(i);
            finished = true;
        }

        if (finished)
            FillSlots();
    }

    private bool UpdateActive(ActiveAnnouncement active, TimeSpan time)
    {
        var display = active.Display;
        var preset = display.Preset;
        var elapsed = time - active.Start;
        var decrypt = display.Title != null && display.Settings.ShowTitle != false
            ? preset.TitleDecrypt
            : TimeSpan.Zero;
        var textElapsed = elapsed - decrypt;
        var holdEnd = decrypt + active.RevealTime + RMCAnnouncementOverlaySystem.GetHoldTime(preset, decrypt, active.RevealTime);
        if (elapsed >= holdEnd + preset.FadeOut)
            return false;

        var titleProgress = decrypt > TimeSpan.Zero ? Math.Clamp((float) (elapsed / decrypt), 0, 1) : 1;
        if (titleProgress < 1 || active.TitleProgress < 1)
        {
            active.TitleProgress = titleProgress;
            ((IRMCAnnouncementView) active.View).SetTitleProgress(titleProgress);
        }

        var text = display.Text;
        var fade = 0;
        var alpha = 1f;
        var power = 1f;
        var typing = false;
        if (preset.PowerOn > TimeSpan.Zero && elapsed < preset.PowerOn)
            power = (float) (elapsed / preset.PowerOn);

        if (textElapsed < TimeSpan.Zero && active.RevealTime > TimeSpan.Zero)
        {
            text = string.Empty;
        }
        else if (textElapsed < active.RevealTime)
        {
            var progress = (float) (textElapsed / active.RevealTime);
            var revealed = (int) (text.Length * progress);
            switch (preset.Animation)
            {
                case TypewriterAnimationConfig:
                    text = text[..revealed];
                    typing = true;
                    break;
                case GlitchAnimationConfig glitch:
                    text = Glitch(text[..revealed], glitch.GlitchChance);
                    typing = true;
                    break;
                case WaveAnimationConfig wave:
                    var head = (int) ((text.Length + wave.FadeChars) * progress);
                    fade = Math.Min(wave.FadeChars, head);
                    text = text[..Math.Clamp(head - wave.FadeChars, 0, text.Length)];
                    break;
                case FadeAnimationConfig:
                    alpha = progress;
                    break;
            }

            if (typing)
                PlayTypingSound(active, revealed);
        }
        else if (elapsed > holdEnd && preset.FadeOut > TimeSpan.Zero)
        {
            var fadeOut = (float) ((elapsed - holdEnd) / preset.FadeOut);
            power = 1f - Math.Clamp(fadeOut / 0.6f, 0, 1);
            alpha = 1f - Math.Clamp((fadeOut - 0.4f) / 0.6f, 0, 1);
        }

        if (!MathHelper.CloseTo(power, active.Power))
        {
            active.Power = power;
            ((IRMCAnnouncementView) active.View).SetPower(power);
        }

        var usesCursor = UsesCursor(preset);
        var cursorVisible = usesCursor && elapsed <= holdEnd && (typing || (int) (elapsed.TotalSeconds * 2) % 2 == 0);
        if (text != active.Shown || fade != active.Fade || cursorVisible != active.CursorVisible)
        {
            active.Shown = text;
            active.Fade = fade;
            active.CursorVisible = cursorVisible;
            ((IRMCAnnouncementView) active.View).SetBody(text, fade, usesCursor, cursorVisible);
        }

        var pulse = 0f;
        if (preset.PulseForever || elapsed < preset.PulseDuration)
        {
            pulse = 0.5f - 0.5f * MathF.Cos((float) elapsed.TotalSeconds * preset.PulseSpeed * MathF.Tau);
            if (!preset.PulseForever)
                pulse *= 1f - (float) (elapsed / preset.PulseDuration);

            pulse *= preset.PulseStrength;
        }

        if (!MathHelper.CloseTo(pulse, active.Pulse))
        {
            active.Pulse = pulse;
            ((IRMCAnnouncementView) active.View).SetPulse(pulse);
        }

        active.View.Modulate = Color.White.WithAlpha(Math.Clamp(alpha, 0, 1));
        return true;
    }

    private static bool UsesCursor(RMCAnnouncementPresetComponent preset)
    {
        return preset.Cursor && preset.Animation is TypewriterAnimationConfig or GlitchAnimationConfig;
    }

    private void PlayTypingSound(ActiveAnnouncement active, int revealed)
    {
        var preset = active.Display.Preset;
        if (preset.TypingSound == null || _audio == null)
            return;

        var step = revealed / Math.Max(1, preset.TypingSoundInterval);
        if (step <= active.SoundStep)
            return;

        active.SoundStep = step;
        _audio.PlayGlobal(preset.TypingSound, Filter.Local(), false);
    }

    private string Glitch(string text, float chance)
    {
        var characters = text.ToCharArray();
        for (var i = 0; i < characters.Length; i++)
        {
            if (!char.IsWhiteSpace(characters[i]) && _random.Prob(chance))
                characters[i] = GlitchCharacters[_random.Next(GlitchCharacters.Length)];
        }

        return new string(characters);
    }

    public static string GetSettingsKey(EntProtoId id, RMCAnnouncementPresetComponent preset)
    {
        return preset.SettingsGroup?.Id ?? id.Id;
    }

    public RMCAnnouncementArea? GetArea()
    {
        var area = _inGameplay ? UIManager.ActiveScreen?.GetOrAddWidget<RMCAnnouncementLayer>().Area : null;
        if (area == _updatingArea)
            return area;

        if (_updatingArea != null)
        {
            _updatingArea.FrameUpdated -= OnAreaFrameUpdate;
            _updatingArea.ScreenScaleChanged -= RefreshActive;
        }

        _updatingArea = area;
        if (area != null)
        {
            area.FrameUpdated += OnAreaFrameUpdate;
            area.ScreenScaleChanged += RefreshActive;
        }

        return area;
    }

    private sealed class ActiveAnnouncement(
        RMCAnnouncementDisplay display,
        Control view,
        RMCAnnouncementArea area,
        TimeSpan start,
        TimeSpan revealTime)
    {
        public readonly RMCAnnouncementDisplay Display = display;
        public readonly Control View = view;
        public readonly RMCAnnouncementArea Area = area;
        public readonly TimeSpan Start = start;
        public readonly TimeSpan RevealTime = revealTime;
        public string Shown = string.Empty;
        public int Fade;
        public float Pulse;
        public bool CursorVisible;
        public float Power = 1;
        public float TitleProgress = 1;
        public int SoundStep;
    }
}
