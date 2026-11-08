using System.Numerics;
using Content.Client.Gameplay;
using Content.Shared._RMC14.Announce;
using Content.Shared._RMC14.Announce.Animations;
using Content.Shared._RMC14.CCVar;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;
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

    public const int MinVisible = 1;
    public const int MaxVisible = 4;
    private const int MaxQueued = 32;
    private const string GlitchCharacters = "#$%&*@!?/|<>{}";

    private readonly List<ActiveAnnouncement> _active = new();
    private readonly List<RMCAnnouncementDisplay> _queue = new();
    private bool _inGameplay;
    private long _nextOrder;
    private int _maxVisible = 2;
    private RMCAnnouncementDisplayMode _mode;
    private Dictionary<string, RMCAnnouncementOverride> _overrides = new();

    public override void Initialize()
    {
        base.Initialize();
        _cfg.OnValueChanged(RMCCVars.RMCAnnouncementMode, OnModeChanged, true);
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

    public IEnumerable<SettingsGroup> GetSettingsGroups()
    {
        foreach (var prototype in _prototype.EnumeratePrototypes<EntityPrototype>())
        {
            if (!prototype.TryGetComponent(out RMCAnnouncementPresetComponent? preset, _compFactory) ||
                preset.SettingsGroup is { } group && group.Id != prototype.ID)
            {
                continue;
            }

            yield return new SettingsGroup(prototype.ID, prototype.Name, preset);
        }
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

    private void OnModeChanged(RMCAnnouncementDisplayMode mode)
    {
        _mode = mode;
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
        var mode = settings.Mode ?? _mode;
        if (mode == RMCAnnouncementDisplayMode.Off)
            return;

        var display = new RMCAnnouncementDisplay
        {
            PresetId = msg.Preset,
            Preset = preset,
            Text = msg.Text,
            Title = msg.Title,
            Accent = msg.Accent ?? preset.AccentColor,
            SpeakerName = msg.SpeakerName,
            Speaker = mode == RMCAnnouncementDisplayMode.Full && settings.ShowPortrait != false ? msg.Portrait : null,
            Effects = mode == RMCAnnouncementDisplayMode.Full,
            Order = _nextOrder++,
        };

        ApplySettings(display, settings);

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
        area.Add(view, display.Pivot);
        UpdateActive(active, _timing.RealTime);
    }

    public static void ApplySettings(RMCAnnouncementDisplay display, RMCAnnouncementOverride settings)
    {
        display.Settings = settings;
        display.Scale = (settings.Scale ?? 1) * display.Preset.Scale;
        display.Pivot = settings.Pivot ?? GetDefaultPivot(display.Preset.Position);
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
        foreach (var active in _active)
        {
            var display = active.Display;
            var settings = RMCAnnouncementOverrides.Resolve(_overrides, display.PresetId.Id, GetSettingsKey(display.PresetId, display.Preset)) with
            {
                Mode = display.Settings.Mode,
            };

            if (settings == display.Settings)
                continue;

            ApplySettings(display, settings);
            active.Area.Remove(active.View);
            ((IRMCAnnouncementView) active.View).Setup(display);
            ((IRMCAnnouncementView) active.View).SetBody(active.Shown);
            active.Area.Add(active.View, display.Pivot);
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
        return display.Effects
            ? RMCAnnouncementOverlaySystem.GetRevealTime(display.Preset, display.Text.Length)
            : TimeSpan.Zero;
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

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
        var holdEnd = active.RevealTime + preset.Hold;
        if (elapsed >= holdEnd + preset.FadeOut)
            return false;

        var text = display.Text;
        var alpha = 1f;
        if (elapsed < active.RevealTime)
        {
            var progress = (float) (elapsed / active.RevealTime);
            switch (preset.Animation)
            {
                case TypewriterAnimationConfig:
                    text = text[..(int) (text.Length * progress)];
                    break;
                case GlitchAnimationConfig glitch:
                    text = Glitch(text[..(int) (text.Length * progress)], glitch.GlitchChance);
                    break;
                case FadeAnimationConfig:
                    alpha = progress;
                    break;
            }
        }
        else if (elapsed > holdEnd && preset.FadeOut > TimeSpan.Zero)
        {
            alpha = 1f - (float) ((elapsed - holdEnd) / preset.FadeOut);
        }

        if (text != active.Shown)
        {
            active.Shown = text;
            ((IRMCAnnouncementView) active.View).SetBody(text);
        }

        active.View.Modulate = Color.White.WithAlpha(Math.Clamp(alpha, 0, 1));
        return true;
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
        if (!_inGameplay)
            return null;

        return UIManager.ActiveScreen?.GetOrAddWidget<RMCAnnouncementLayer>().Area;
    }

    public readonly record struct SettingsGroup(string Id, string Name, RMCAnnouncementPresetComponent Preset);

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
    }
}
