using System.Text.RegularExpressions;
using Content.Client._RMC14.Xenonids.UI;
using Content.Shared._RMC14.Xenonids.Watch;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.LineEdit;

namespace Content.Client._RMC14.Xenonids.Watch;

[UsedImplicitly]
public sealed class XenoWatchBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    [ViewVariables]
    private XenoWatchWindow? _window;

    private readonly SpriteSystem _sprite;

    public XenoWatchBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sprite = EntMan.System<SpriteSystem>();
    }

    protected override void Open()
    {
        base.Open();
        EnsureWindow();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not XenoWatchBuiState s)
            return;

        _window = EnsureWindow();
        _window.BurrowedLarvaLabel.Text = $"Burrowed Larva: {s.BurrowedLarva}";
        _window.XenoContainer.DisposeAllChildren();

        foreach (var xeno in s.Xenos)
        {
            Texture? texture = null;
            if (xeno.Id != null &&
                _prototype.TryIndex(xeno.Id.Value, out var evolution))
            {
                texture = _sprite.Frame0(evolution);
            }

            var control = new XenoChoiceControl();
            control.Set(xeno.Name, texture);
            control.Button.OnPressed += _ => SendPredictedMessage(new XenoWatchBuiMsg(xeno.Entity));

            _window.XenoContainer.AddChild(control);
        }
    }

    private XenoWatchWindow EnsureWindow()
    {
        if (_window != null)
            return _window;

        _window = this.CreateWindow<XenoWatchWindow>();
        _window.SearchBar.OnTextChanged += OnSearchBarChanged;
        return _window;
    }

    private static readonly Regex DashesAndDigitsPattern = new Regex("[-\\d]+");

    /// <summary>
    /// <c>RemoveDashesAndDigits</c> returns a copy of the input string with all hyphen <c>-</c> and digit <c>0-9</c>
    /// characters removed. This allows for a smarter search in the Watch search menus.
    /// <param name="name">The name of a humanoid or xenonid character</param>
    /// <example>
    /// <c>"KE-112-E"</c> returns <c>"KEE"</c>,
    /// <c>"XX-42"</c> returns <c>"XX"</c>,
    /// <c>"HEK"</c> returns <c>"HEK"</c>,
    /// <c>"HEK-891"</c> returns <c>"HEK"</c>,
    /// <c>"RO-123-CK"</c> returns <c>"ROCK"</c>,
    /// <c>"Shakes-The-Friendlies"</c> returns <c>"ShakesTheFriendlies"</c>,
    /// <c>"Zachary Randolf"</c> returns <c>"Zachary Randolf"</c>.
    /// </example>
    /// </summary>
    public static string RemoveDashesAndDigits(string name)
    {
        return DashesAndDigitsPattern.Replace(name, "");
    }

    private bool ShowSearchResult(string search, string result)
    {
        return result.Contains(search, StringComparison.OrdinalIgnoreCase)
               || RemoveDashesAndDigits(result).Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private void OnSearchBarChanged(LineEditEventArgs args)
    {
        if (_window is not { Disposed: false })
            return;

        foreach (var child in _window.XenoContainer.Children)
        {
            if (child is not XenoChoiceControl control)
                continue;

            if (string.IsNullOrWhiteSpace(args.Text))
                control.Visible = true;
            else
                control.Visible = ShowSearchResult(args.Text, control.NameLabel.GetMessage() ?? "");
        }
    }
}
