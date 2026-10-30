using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.UserInterface;

[Virtual]
public class RMCMachineButton : Button
{
    private readonly Font _font;
    private RMCIconLabelRow? _iconRow;
    private Color _accent = RMCMachineUiStyle.ButtonColor;
    private bool _secondary;

    private readonly RMCBevelStyleBox _box = new()
    {
        ContentMarginLeftOverride = 8,
        ContentMarginRightOverride = 8,
        ContentMarginTopOverride = 3,
        ContentMarginBottomOverride = 3,
    };

    private readonly StyleBoxFlat _secondaryBox = new()
    {
        BorderThickness = new Thickness(1),
        ContentMarginLeftOverride = 6,
        ContentMarginRightOverride = 6,
        ContentMarginTopOverride = 2,
        ContentMarginBottomOverride = 2,
    };

    public RMCMachineButton()
    {
        _font = RMCMachineUiStyle.ButtonFont(IoCManager.Resolve<IResourceCache>());
        StyleBoxOverride = _box;
        ModulateSelfOverride = Color.White;
        Label.FontOverride = _font;
        UpdateStyle();
    }

    public Color Accent
    {
        get => _accent;
        set
        {
            _accent = value;
            UpdateStyle();
        }
    }

    public bool Secondary
    {
        get => _secondary;
        set
        {
            _secondary = value;
            StyleBoxOverride = value ? _secondaryBox : _box;
            UpdateStyle();
        }
    }

    public RMCIconLabelRow SetIcon(string path, string? text = null, float size = 14)
    {
        if (_iconRow != null)
        {
            _iconRow.SetIcon(path);
            if (text != null)
                _iconRow.Label.Text = text;

            return _iconRow;
        }

        _iconRow = this.AddIconLabel(path, text ?? Text ?? string.Empty, size, HAlignment.Center);
        _iconRow.Label.FontOverride = _font;
        UpdateStyle();
        return _iconRow;
    }

    protected override void DrawModeChanged()
    {
        base.DrawModeChanged();
        UpdateStyle();
    }

    private void UpdateStyle()
    {
        if (_secondary)
        {
            UpdateSecondaryStyle();
            return;
        }

        _box.BackgroundColor = DrawMode switch
        {
            DrawModeEnum.Disabled => RMCMachineUiStyle.ButtonDisabledColor,
            DrawModeEnum.Pressed => Color.InterpolateBetween(_accent, Color.Black, 0.15f),
            DrawModeEnum.Hover => Color.InterpolateBetween(_accent, Color.White, 0.15f),
            _ => _accent,
        };

        var text = RMCMachineUiStyle.ButtonTextColor;
        if (Label != null)
            Label.FontColorOverride = text;

        if (_iconRow == null)
            return;

        _iconRow.Label.FontColorOverride = text;
        _iconRow.Icon.IdleColor = text;
        _iconRow.Icon.HoverColor = DrawMode == DrawModeEnum.Disabled ? text : RMCIcons.Hover;
    }

    private void UpdateSecondaryStyle()
    {
        var (background, border, text) = DrawMode switch
        {
            DrawModeEnum.Disabled => (RMCMachineUiStyle.FieldColor, RMCMachineUiStyle.ButtonDisabledColor, RMCMachineUiStyle.ButtonDisabledColor),
            DrawModeEnum.Pressed => (_accent, _accent, RMCMachineUiStyle.ButtonTextColor),
            DrawModeEnum.Hover => (Color.InterpolateBetween(RMCMachineUiStyle.FieldColor, _accent, 0.35f), RMCMachineUiStyle.ButtonSelectedColor, Color.White),
            _ => (RMCMachineUiStyle.FieldColor, _accent, RMCMachineUiStyle.ButtonSelectedColor),
        };

        _secondaryBox.BackgroundColor = background;
        _secondaryBox.BorderColor = border;

        if (Label != null)
            Label.FontColorOverride = text;

        if (_iconRow == null)
            return;

        _iconRow.Label.FontColorOverride = text;
        _iconRow.Icon.IdleColor = text;
        _iconRow.Icon.HoverColor = text;
    }
}
