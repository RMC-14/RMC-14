using System.Globalization;
using System.Numerics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.UserInterface;

public sealed class RMCMachineSpinBox : BoxContainer
{
    private readonly LineEdit _lineEdit;
    private readonly float _step;
    private readonly byte _precision;
    private float _value;

    public Func<float, bool>? IsValid { get; set; }

    public event Action<float>? OnValueChanged;

    public RMCMachineSpinBox(float value = 0, float step = 1, byte precision = 0, int minWidth = 130)
    {
        Orientation = LayoutOrientation.Horizontal;
        MinWidth = minWidth;
        SeparationOverride = 2;

        _step = MathF.Max(step, MathF.Pow(10, -precision));
        _precision = precision;

        AddChild(CreateStepButton("-", -_step));

        _lineEdit = new LineEdit
        {
            MinSize = new Vector2(40, 0),
            HorizontalExpand = true,
            StyleBoxOverride = RMCMachineUiStyle.Field(),
        };
        _lineEdit.OnFocusExit += TextChanged;
        _lineEdit.OnTextEntered += TextChanged;
        AddChild(_lineEdit);

        AddChild(CreateStepButton("+", _step));

        _value = value;
        UpdateText();
    }

    public float Value
    {
        get => _value;
        set
        {
            if (IsValid != null && !IsValid(value))
                return;

            _value = value;
            UpdateText();
        }
    }

    public override bool HasKeyboardFocus()
    {
        return _lineEdit.HasKeyboardFocus();
    }

    private RMCMachineButton CreateStepButton(string text, float change)
    {
        var button = new RMCMachineButton
        {
            Text = text,
            MinWidth = 24,
            TextAlign = Label.AlignMode.Center,
        };
        button.OnPressed += _ => TrySet(_value + change);
        return button;
    }

    private void TextChanged(LineEdit.LineEditEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Text))
        {
            TrySet(0);
            UpdateText();
            return;
        }

        if (float.TryParse(args.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            TrySet(MathF.Round(parsed, _precision));

        UpdateText();
    }

    private void TrySet(float value)
    {
        if (IsValid != null && !IsValid(value))
            return;

        _value = value;
        UpdateText();
        OnValueChanged?.Invoke(_value);
    }

    private void UpdateText()
    {
        var cursor = _lineEdit.CursorPosition;
        _lineEdit.Text = _value.ToString("F" + _precision, CultureInfo.InvariantCulture);
        _lineEdit.CursorPosition = Math.Min(cursor, _lineEdit.Text.Length);
    }
}
