using System.Globalization;
using System.Linq;
using System.Numerics;
using Content.Client.Gameplay;
using Content.Shared._RMC14.Announce;
using Content.Shared._RMC14.CCVar;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Announce;

public sealed class RMCAnnouncementLayoutUIController : UIController, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private const float WheelScaleStep = 0.1f;

    private readonly List<EditablePreset> _presets = new();
    private Dictionary<string, RMCAnnouncementOverride> _working = new();
    private RMCAnnouncementEditWindow? _window;
    private RMCAnnouncementEditFrame? _frame;
    private RMCAnnouncementArea? _area;
    private int _index;
    private bool _editGlobal;
    private bool _populating;

    private RMCAnnouncementUIController Announcements => UIManager.GetUIController<RMCAnnouncementUIController>();

    private EditablePreset Preset => _presets[_index];

    private string TargetKey => _editGlobal ? RMCAnnouncementOverrides.GlobalKey : Preset.Id;

    private RMCAnnouncementOverride Target
    {
        get => _working.GetValueOrDefault(TargetKey);
        set => _working[TargetKey] = value;
    }

    private RMCAnnouncementOverride Fallback
    {
        get
        {
            if (_editGlobal)
                return default;

            var global = _working.GetValueOrDefault(RMCAnnouncementOverrides.GlobalKey);
            return Preset.Id == Preset.GroupId
                ? global
                : _working.GetValueOrDefault(Preset.GroupId).Over(global);
        }
    }

    private RMCAnnouncementOverride Effective => Target.Over(Fallback);

    public override void Initialize()
    {
        base.Initialize();
        UIManager.OnScreenChanged += _ => _window?.Close();
    }

    public void OnStateExited(GameplayState state)
    {
        _window?.Close();
    }

    public bool TryOpen()
    {
        if (_window != null)
        {
            _window.MoveToFront();
            return true;
        }

        if (Announcements.GetArea() == null)
            return false;

        LoadPresets();
        if (_presets.Count == 0)
            return false;

        _working = RMCAnnouncementOverrides.Parse(_cfg.GetCVar(RMCCVars.RMCAnnouncementOverrides));
        _index = 0;
        _editGlobal = false;

        _window = UIManager.CreateWindow<RMCAnnouncementEditWindow>();
        _window.OnClose += OnWindowClosed;
        ConfigureWindow(_window);
        _window.OpenCentered();

        Refresh();
        return true;
    }

    public void ResetAll()
    {
        var overrides = RMCAnnouncementOverrides.Parse(_cfg.GetCVar(RMCCVars.RMCAnnouncementOverrides));
        foreach (var (id, value) in overrides.ToArray())
        {
            overrides[id] = value.WithoutLayout();
        }

        _cfg.SetCVar(RMCCVars.RMCAnnouncementOverrides, RMCAnnouncementOverrides.Serialize(overrides));
        _cfg.SaveToFile();
    }

    private void LoadPresets()
    {
        _presets.Clear();
        foreach (var prototype in _prototype.EnumeratePrototypes<EntityPrototype>())
        {
            if (!prototype.TryGetComponent(out RMCAnnouncementPresetComponent? preset, _compFactory) ||
                !preset.ShowInLayoutEditor)
            {
                continue;
            }

            var groupId = RMCAnnouncementUIController.GetSettingsKey(prototype.ID, preset);
            _presets.Add(new EditablePreset(prototype.ID, groupId, prototype.Name, preset));
        }

        _presets.Sort((a, b) =>
        {
            var group = string.CompareOrdinal(a.GroupId, b.GroupId);
            return group != 0 ? group : string.CompareOrdinal(a.Id, b.Id);
        });
    }

    private void ConfigureWindow(RMCAnnouncementEditWindow window)
    {
        var duplicates = _presets
            .GroupBy(preset => preset.Name)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet();

        for (var i = 0; i < _presets.Count; i++)
        {
            var preset = _presets[i];
            var label = duplicates.Contains(preset.Name) ? $"{preset.Name} ({preset.Id})" : preset.Name;
            window.PreviewPresetButton.AddItem(label, i);
        }

        window.PreviewPresetButton.OnItemSelected += args =>
        {
            window.PreviewPresetButton.SelectId(args.Id);
            _index = args.Id;
            Refresh();
        };

        window.GlobalCheckBox.OnToggled += args =>
        {
            _editGlobal = args.Pressed;
            Refresh();
        };

        BindNumber(window.PositionXInput, value => SetPivot(x: value));
        BindNumber(window.PositionYInput, value => SetPivot(y: value));
        BindNumber(window.ScaleInput, value => Change(Target with { Scale = ClampScale(value) }));
        BindNumber(window.BodyScaleInput, value => Change(Target with { BodyScale = ClampScale(value) }));
        BindNumber(window.TitleScaleInput, value => Change(Target with { TitleScale = ClampScale(value) }));

        window.ScaleSlider.OnValueChanged += _ =>
        {
            if (!_populating)
                Change(Target with { Scale = ClampScale(window.ScaleSlider.Value) });
        };

        window.ShowTitleCheckBox.OnToggled += args => Change(Target with { ShowTitle = args.Pressed });
        window.ShowPortraitCheckBox.OnToggled += args => Change(Target with { ShowPortrait = args.Pressed });
        window.ShowAnnouncementCheckBox.OnToggled += args => Change(Target with { Show = args.Pressed });

        BindColor(window.BodyColorInput, window.BodyColorPicker, color => Target with { TextColor = color });
        BindColor(window.TitleColorInput, window.TitleColorPicker, color => Target with { TitleColor = color });
        BindColor(window.SpriteBoxColorInput, window.SpriteBoxColorPicker, color => Target with { PortraitBackgroundColor = color });
        BindColor(window.SpriteBoxBorderColorInput, window.SpriteBoxBorderColorPicker, color => Target with { PortraitBorderColor = color });
        BindColor(window.CRTGlowColorInput, window.CRTGlowColorPicker, color => Target with { CrtGlowColor = color });
        BindColor(window.BackgroundColorInput, window.BackgroundColorPicker, color => Target with { BackgroundColor = color });

        window.ResetTargetButton.OnPressed += _ =>
        {
            Target = Target.WithoutLayout();
            Commit();
            Refresh();
        };

        window.ResetAllButton.OnPressed += _ =>
        {
            ResetAll();
            _working = RMCAnnouncementOverrides.Parse(_cfg.GetCVar(RMCCVars.RMCAnnouncementOverrides));
            Refresh();
        };
    }

    private void BindNumber(LineEdit input, Action<float> apply)
    {
        void OnCommit(LineEdit.LineEditEventArgs args)
        {
            if (_populating)
                return;

            if (float.TryParse(args.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                apply(value);
            else
                Populate();
        }

        input.OnTextEntered += OnCommit;
        input.OnFocusExit += OnCommit;
    }

    private void BindColor(LineEdit input, ColorSelectorSliders picker, Func<Color?, RMCAnnouncementOverride> apply)
    {
        void OnCommit(LineEdit.LineEditEventArgs args)
        {
            if (_populating)
                return;

            var text = args.Text.Trim();
            if (text.Length == 0)
            {
                Change(apply(null));
                return;
            }

            if (Color.TryFromHex(text) is { } color)
                Change(apply(color));
            else
                Populate();
        }

        input.OnTextEntered += OnCommit;
        input.OnFocusExit += OnCommit;

        picker.OnColorChanged += color =>
        {
            if (_populating)
                return;

            input.Text = color.ToHex();
            Change(apply(color));
        };
    }

    private static float ClampScale(float value)
    {
        return Math.Clamp(value, RMCAnnouncementOverrides.MinScale, RMCAnnouncementOverrides.MaxScale);
    }

    private void SetPivot(float? x = null, float? y = null)
    {
        var pivot = Effective.Pivot ?? RMCAnnouncementUIController.GetDefaultPivot(Preset.Preset.Position);
        pivot = Vector2.Clamp(new Vector2(x ?? pivot.X, y ?? pivot.Y), Vector2.Zero, Vector2.One);
        Target = Target with { Pivot = pivot };
        Commit();

        if (_frame != null)
            _area?.MoveExclusive(_frame, pivot);

        Populate();
    }

    private void Change(RMCAnnouncementOverride value)
    {
        Target = value;
        Commit();
        Refresh();
    }

    private void Commit()
    {
        _cfg.SetCVar(RMCCVars.RMCAnnouncementOverrides, RMCAnnouncementOverrides.Serialize(_working));
    }

    private void Refresh()
    {
        Populate();
        ShowPreview();
    }

    private void Populate()
    {
        if (_window == null)
            return;

        _populating = true;

        var preset = Preset.Preset;
        var target = Target;
        var effective = Effective;
        var pivot = effective.Pivot ?? RMCAnnouncementUIController.GetDefaultPivot(preset.Position);
        var scale = effective.Scale ?? 1;
        var accent = preset.AccentColor;

        _window.PreviewPresetButton.SelectId(_index);
        _window.GlobalCheckBox.Pressed = _editGlobal;

        _window.ScaleValueLabel.Text = Loc.GetString("rmc-ui-options-announcements-layout-scale-value",
            ("value", scale.ToString("0.00", CultureInfo.InvariantCulture) + "x"));

        _window.PositionXInput.Text = pivot.X.ToString("0.000", CultureInfo.InvariantCulture);
        _window.PositionYInput.Text = pivot.Y.ToString("0.000", CultureInfo.InvariantCulture);
        _window.ScaleInput.Text = scale.ToString("0.000", CultureInfo.InvariantCulture);
        _window.ScaleSlider.SetValueWithoutEvent(scale);
        _window.BodyScaleInput.Text = (effective.BodyScale ?? 1).ToString("0.000", CultureInfo.InvariantCulture);
        _window.TitleScaleInput.Text = (effective.TitleScale ?? 1).ToString("0.000", CultureInfo.InvariantCulture);

        var hasPortrait = _editGlobal || preset.Template == RMCAnnouncementTemplate.Portrait;
        _window.ShowTitleCheckBox.Pressed = effective.ShowTitle ?? true;
        _window.ShowPortraitCheckBox.Disabled = !hasPortrait;
        _window.ShowPortraitCheckBox.Pressed = hasPortrait && (effective.ShowPortrait ?? true);
        _window.ShowAnnouncementCheckBox.Pressed = effective.Show ?? true;

        var showCrt = _editGlobal || preset.Crt != null;
        var showBackground = _editGlobal || preset.BackgroundColor != null;
        _window.SpriteBoxColorRow.Visible = hasPortrait && effective.ShowPortrait != false;
        _window.CRTGlowColorSection.Visible = showCrt;
        _window.BackgroundColorSection.Visible = showBackground;
        _window.ExtraColorRow.Visible = showCrt || showBackground;

        SetColor(_window.BodyColorInput, _window.BodyColorPicker, target.TextColor, effective.TextColor ?? preset.TextColor ?? accent);
        SetColor(_window.TitleColorInput, _window.TitleColorPicker, target.TitleColor, effective.TitleColor ?? accent);
        SetColor(_window.SpriteBoxColorInput, _window.SpriteBoxColorPicker, target.PortraitBackgroundColor, effective.PortraitBackgroundColor ?? preset.PortraitBackgroundColor);
        SetColor(_window.SpriteBoxBorderColorInput, _window.SpriteBoxBorderColorPicker, target.PortraitBorderColor, effective.PortraitBorderColor ?? accent);
        SetColor(_window.CRTGlowColorInput, _window.CRTGlowColorPicker, target.CrtGlowColor, effective.CrtGlowColor ?? accent);
        SetColor(_window.BackgroundColorInput, _window.BackgroundColorPicker, target.BackgroundColor, effective.BackgroundColor ?? preset.BackgroundColor ?? Color.Black);

        _window.ResetTargetButton.Disabled = !target.HasLayout;
        _window.PositionValueLabel.Text = target.Pivot is { } targetPivot
            ? Loc.GetString("rmc-ui-options-announcements-layout-position-value",
                ("x", targetPivot.X.ToString("0.000", CultureInfo.InvariantCulture)),
                ("y", targetPivot.Y.ToString("0.000", CultureInfo.InvariantCulture)))
            : Loc.GetString("rmc-ui-options-announcements-layout-position-default");

        _populating = false;
    }

    private static void SetColor(LineEdit input, ColorSelectorSliders picker, Color? value, Color shown)
    {
        input.Text = value?.ToHex() ?? string.Empty;
        picker.Color = shown;
    }

    private void ShowPreview()
    {
        if (_window == null || Announcements.GetArea() is not { } area)
            return;

        if (_frame != null)
            _area?.Remove(_frame);

        _area = area;

        var preset = Preset;
        var local = _player.LocalEntity;
        var text = string.Join('\n',
            Loc.GetString("rmc-ui-options-announcements-layout-preview-line-1", ("name", preset.Name)),
            Loc.GetString("rmc-ui-options-announcements-layout-preview-line-2"));

        var speakerName = Loc.GetString("rmc-ui-options-announcements-layout-preview-speaker");
        string? job = null;
        if (local is { } localEntity)
            (speakerName, job) = _entities.System<RMCAnnouncementOverlaySystem>().GetSpeakerInfo(localEntity);

        var display = new RMCAnnouncementDisplay
        {
            PresetId = preset.Id,
            Preset = preset.Preset,
            Text = text,
            Title = RMCAnnouncementUIController.GetTitle(preset.Preset, null),
            Accent = preset.Preset.AccentColor,
            SpeakerName = speakerName,
            SpeakerJob = job,
            Portrait = preset.Preset.Template == RMCAnnouncementTemplate.Portrait ? local : null,
        };

        RMCAnnouncementUIController.ApplySettings(display, Effective, area.ScreenScale);

        var view = RMCAnnouncementUIController.CreateView(display);
        ((IRMCAnnouncementView) view).SetBody(display.Text, 0, false, false);

        _frame = new RMCAnnouncementEditFrame(view);
        _frame.Dragged += OnDragged;
        _frame.Scrolled += OnScrolled;
        _frame.Modulate = Color.White.WithAlpha(Effective.Show == false ? 0.35f : 1f);
        area.Add(_frame, display.Pivot, display.AvoidHud, exclusive: true);
    }

    private void OnDragged(Vector2 topLeft)
    {
        if (_frame == null || _area == null)
            return;

        var pivot = _area.GetPivot(topLeft, _frame.Size);
        Target = Target with { Pivot = pivot };
        Commit();
        _area.MoveExclusive(_frame, pivot);
        Populate();
    }

    private void OnScrolled(float delta)
    {
        Change(Target with { Scale = ClampScale((Effective.Scale ?? 1) + MathF.Sign(delta) * WheelScaleStep) });
    }

    private void OnWindowClosed()
    {
        if (_frame != null)
            _area?.Remove(_frame);

        _cfg.SaveToFile();
        _frame = null;
        _area = null;
        _window = null;
    }

    private readonly record struct EditablePreset(string Id, string GroupId, string Name, RMCAnnouncementPresetComponent Preset);
}
