using System.Linq;
using Content.Client._RMC14.Xenonids.UI;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Strain;
using Content.Shared._RMC14.Xenonids.Watch;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.LineEdit;

namespace Content.Client._RMC14.Xenonids.Watch;

[UsedImplicitly]
public sealed class XenoWatchBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;

    private static readonly Comparer<EntityPrototype> EntityComparer =
        Comparer<EntityPrototype>.Create(static (a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
    private static readonly Color XenoColor = Color.FromHex("#800080");
    private static readonly Color XenoHighlightColor = Color.FromHex("#9E379E");
    private SortedDictionary<int, SortedSet<EntityPrototype>> tiers = new();
    private Dictionary<string, bool> ShownXenos = new();
    private bool UsingSearchBar = false;

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
        _window = EnsureWindow();


        foreach (var entity in _prototype.EnumeratePrototypes<EntityPrototype>())
        {
            if (entity.Abstract || !entity.TryGetComponent(out XenoComponent? xeno, _compFactory))
                continue;

            if (entity.TryGetComponent(out XenoStrainComponent? strain, _compFactory) || xeno.Hidden)
                continue;

            if (!tiers.TryGetValue(xeno.Tier, out var xenos))
            {
                xenos = new SortedSet<EntityPrototype>(EntityComparer);
                tiers.Add(xeno.Tier, xenos);
            } ;

            xenos.Add(entity);
        }

        foreach (var (tier, xenos) in tiers.Reverse())
        {
            if (tier > 3) // i dont think queen shows on the ss13 one
                continue;

            var row = new XenoTierRow();
            row.TierButton.OnToggled += OnTierToggled;
            row.SetInfo(0, tier);
            foreach (var xeno in xenos)
            {
                //row.AddXeno(0, xeno.Name);

                var xenocontrol = new XenoHiveCountControl();
                var xenobutton = new Button();

                xenobutton.Text = xeno.Name;
                xenobutton.Name = xeno.Name;
                xenobutton.ToggleMode = true;
                xenobutton.ModulateSelfOverride = Color.Purple;
                xenobutton.AddStyleClass("ButtonSquare");

                xenobutton.OnToggled += OnButtonToggled;
                xenocontrol.XenoButton.AddChild(xenobutton);

                row.XenosContainer.AddChild(xenocontrol);
                ShownXenos.Add(xeno.Name, false);
            }
            _window.RowContainer.AddChild(row);
        }

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

    private void OnSearchBarChanged(LineEditEventArgs args)
    {
        if (_window is not { Disposed: false })
            return;

        foreach (var child in _window.XenoContainer.Children)
        {
            if (child is not XenoChoiceControl control)
                continue;

            if (string.IsNullOrWhiteSpace(args.Text))
            {
                control.Visible = true;
                UsingSearchBar = false;
                UpdateList();
            }
            else
            {
                UsingSearchBar = true;
                control.Visible =
                    control.NameLabel.GetMessage()?.Contains(args.Text, StringComparison.OrdinalIgnoreCase) ?? false;
            }
        }
    }

    private void OnButtonToggled(BaseButton.ButtonEventArgs args)
    {
        string name = args.Button.Name ?? string.Empty; // button name is emtpy, what i need is the text

        if (ShownXenos.ContainsKey(name))
        {
            ShownXenos[name] = !ShownXenos[name];
            args.Button.ModulateSelfOverride = UpdateButtonColor(ShownXenos[name]);
        }

        Logger.Debug(name);
        ClearSearchBar();
        UpdateList();
    }

    private void OnTierToggled(BaseButton.ButtonEventArgs args)
    {
        args.Button.ModulateSelfOverride = UpdateButtonColor(args.Button.Pressed);

        if (args.Button.Name != null && int.TryParse(args.Button.Name.Split()[1], out var number)) // get the tier number frmo the button name
        {
            foreach (var xeno in tiers[number])
            {
                ShownXenos[xeno.Name] = args.Button.Pressed;
                //Logger.Debug(xeno.Name);
            }
        }
        UpdateTierChilds();
        ClearSearchBar();
        UpdateList();
    }

    private void UpdateTierChilds()
    {
        if (_window is not { Disposed: false })
            return;

        foreach (var child in _window.RowContainer.Children)
        {
            if (child is not XenoTierRow control)
                continue;
            foreach (var xenocountchild in control.XenosContainer.Children) //i love nested loops (theres probably a better way to do this)
            {
                if (xenocountchild is not XenoHiveCountControl countControl)
                    continue;

                foreach (var button in countControl.XenoButton.Children)
                {
                    if (button is not Button xenobutton)
                        continue;
                    var name = xenobutton.Name ?? "";
                    if (name is not "")
                        xenobutton.ModulateSelfOverride = UpdateButtonColor(ShownXenos[name]);
                    //Logger.Debug($"UpdateTier, state is now: {ShownXenos[name]}");
                }
            }
        }
    }

    private Color UpdateButtonColor(bool state)
    {
        return state ? XenoHighlightColor : XenoColor;
    }

    private void ClearSearchBar()
    {
        if (_window is not { Disposed: false })
            return;

        _window.SearchBar.Clear();
        UsingSearchBar = false;
    }

    private void UpdateList()
    {
        if (_window is not { Disposed: false })
            return;

        if (!UsingSearchBar)
        {
            foreach (var child in _window.XenoContainer.Children)
            {
                if (child is not XenoChoiceControl control)
                    continue;

                if (!ShownXenos.ContainsValue(true))
                {
                    control.Visible = true;
                    continue;
                }

                if (ShownXenos.TryGetValue(control.NameLabel.GetMessage() ?? "", out var xeno))
                    control.Visible = xeno;
            }
        }
    }
}
