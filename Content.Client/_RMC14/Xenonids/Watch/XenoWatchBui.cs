using Content.Client._RMC14.Xenonids.UI;
using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Xenonids.Heal;
using Content.Shared._RMC14.Xenonids.Watch;
using Content.Shared.Damage;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using SDL3;
using static Robust.Client.UserInterface.Controls.LineEdit;

namespace Content.Client._RMC14.Xenonids.Watch;

[UsedImplicitly]
public sealed class XenoWatchBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    [ViewVariables]
    private XenoWatchWindow? _window;

    private readonly SpriteSystem _sprite;
    private readonly HashSet<int> _selectedTiers = new HashSet<int>();
    private readonly HashSet<string> _selectedButtons = new HashSet<string>();


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


        _window.SetTier1Count(s.TierOne);
        _window.SetTier2Count(s.TierTwo);
        _window.SetTier3Count(s.TierThree);
        _window.SetTier2Slots(s.TierTwoSlots);
        _window.SetTier3Slots(s.TierThreeSlots);
        _window.SetXenoCount(s.XenoCount);


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
            //Logger.Debug(xeno.Health.ToString());
            //Logger.Debug(xeno.Plasma.ToString());
            control.SetHealth(xeno.Health);
            control.SetPlasma(xeno.Plasma);
            control.SetEvo(xeno.Evo);
            _window.XenoContainer.AddChild(control);
            UpdateList();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _window?.Dispose();
    }

    private XenoWatchWindow EnsureWindow()
    {
        if (_window != null)
            return _window;
        _window = new XenoWatchWindow();
        _window.OnClose += Close;
        _window.SearchBar.OnTextChanged += OnSearchBarChanged;
        _window.Tier0.OnToggled += OnTierToggled;
        _window.Tier1.OnToggled += OnTierToggled;
        _window.Tier2.OnToggled += OnTierToggled;
        _window.Tier3.OnToggled += OnTierToggled;
        _window.Tier4.OnToggled += OnTierToggled;
        _window.Crusher.OnToggled += OnButtonToggled;
        _window.Ravager.OnToggled += OnButtonToggled;
        _window.Boiler.OnToggled += OnButtonToggled;
        _window.Praetorian.OnToggled += OnButtonToggled;
        _window.Spitter.OnToggled += OnButtonToggled;
        _window.Carrier.OnToggled += OnButtonToggled;
        _window.Hivelord.OnToggled += OnButtonToggled;
        _window.Burrower.OnToggled += OnButtonToggled;
        _window.Warrior.OnToggled += OnButtonToggled;
        _window.Lurker.OnToggled += OnButtonToggled;
        _window.Defender.OnToggled += OnButtonToggled;
        _window.Drone.OnToggled += OnButtonToggled;
        _window.Sentinel.OnToggled += OnButtonToggled;
        _window.Runner.OnToggled += OnButtonToggled;
        _window.Queen.OnToggled += OnButtonToggled;
        _window.Parasite.OnToggled += OnButtonToggled;
        _window.Lesser.OnToggled += OnButtonToggled;

        _window.OpenCentered();
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
                control.Visible = true;
            else
                control.Visible = control.NameLabel.GetMessage()?.Contains(args.Text, StringComparison.OrdinalIgnoreCase) ?? false;
        }
    }


    private void OnButtonToggled(BaseButton.ButtonEventArgs args)
    {
        if (_window is not { Disposed: false })
            return;

        if (args.Button.Name is null)
        {
            //Logger.Debug("Button name is null");
            return;
        }

        _window._buttons[args.Button.Name] = !_window._buttons[args.Button.Name];
        /*
        Logger.Debug("Button pressed is");
        Logger.Debug(args.Button.Name ?? string.Empty);
        Logger.Debug("Button State is:");
        Logger.Debug(_window._buttons[args.Button.Name ?? string.Empty] == true ? "True" : "False");
        */

        _window.Tier0.Pressed = false;
        _window.Tier1.Pressed = false;
        _window.Tier2.Pressed = false;
        _window.Tier3.Pressed = false;
        _window.Tier4.Pressed = false;
        _window._buttons["Tier0"] = false;
        _window._buttons["Tier1"] = false;
        _window._buttons["Tier2"] = false;
        _window._buttons["Tier3"] = false;
        _window._buttons["Tier4"] = false;

        UpdateList();
    }

    private void OnTierToggled(BaseButton.ButtonEventArgs args)
    {
        if (_window is not { Disposed: false })
            return;

        //Logger.Debug(args.Button.Name ?? "Null");
        switch (args.Button.Name)
        {
            case "Tier0":
                _window.Lesser.Pressed = args.Button.Pressed;
                _window._buttons["Lesser"] = _window.Lesser.Pressed;
                _window.Parasite.Pressed = args.Button.Pressed;
                _window._buttons["Parasite"] = _window.Parasite.Pressed;
                break;
            case "Tier1":
                _window.Drone.Pressed = args.Button.Pressed;
                _window._buttons["Drone"] = _window.Drone.Pressed;
                _window.Defender.Pressed = args.Button.Pressed;
                _window._buttons["Defender"] = _window.Defender.Pressed;
                _window.Sentinel.Pressed = args.Button.Pressed;
                _window._buttons["Sentinel"] = _window.Sentinel.Pressed;
                _window.Runner.Pressed = args.Button.Pressed;
                _window._buttons["Runner"] = _window.Runner.Pressed;
                break;
            case "Tier2":
                _window.Spitter.Pressed = args.Button.Pressed;
                _window._buttons["Spitter"] = _window.Spitter.Pressed;
                _window.Warrior.Pressed = args.Button.Pressed;
                _window._buttons["Warrior"] = _window.Warrior.Pressed;
                _window.Burrower.Pressed = args.Button.Pressed;
                _window._buttons["Burrower"] = _window.Burrower.Pressed;
                _window.Carrier.Pressed = args.Button.Pressed;
                _window._buttons["Carrier"] = _window.Carrier.Pressed;
                _window.Hivelord.Pressed = args.Button.Pressed;
                _window._buttons["Hivelord"] = _window.Hivelord.Pressed;
                _window.Lurker.Pressed = args.Button.Pressed;
                _window._buttons["Lurker"] = _window.Lurker.Pressed;
                break;
            case "Tier3":
                _window.Ravager.Pressed = args.Button.Pressed;
                _window._buttons["Ravager"] = _window.Ravager.Pressed;
                _window.Crusher.Pressed = args.Button.Pressed;
                _window._buttons["Crusher"] = _window.Crusher.Pressed;
                _window.Praetorian.Pressed = args.Button.Pressed;
                _window._buttons["Praetorian"] = _window.Praetorian.Pressed;
                _window.Boiler.Pressed = args.Button.Pressed;
                _window._buttons["Boiler"] = _window.Boiler.Pressed;
                break;
            case "Tier4":
                _window.Queen.Pressed = args.Button.Pressed;
                _window._buttons["Queen"] = _window.Queen.Pressed;
                break;
            default:
                //Logger.Debug("Yeah this is running, its not going for a tier");
                break;
        }
        UpdateList();
    }

    private void UpdateList()
    {
        if (_window is not { Disposed: false })
            return;

        bool dontshowall = false; // will only show all xenos if all buttons are not pressed

        foreach (bool state in _window._buttons.Values)
        {
            dontshowall = state || dontshowall; // if a single one is true dontshowall will become true
        }

        foreach (var child in _window.XenoContainer.Children)
        {
            if (child is not XenoChoiceControl control)
                continue;
            if (control.Button.Name is null)
                break;
            control.Visible = IsXenoVisible(control) || !dontshowall;
        }
    }

    private bool IsXenoVisible(XenoChoiceControl control)
    {
        bool value = false;
        if (_window is not { Disposed: false })
            return false;
        //Logger.Debug("IsXenoVisible called, control name is");
        //Logger.Debug(control.NameLabel.GetMessage() ?? string.Empty);

        string xenoname = control.NameLabel.GetMessage() ?? string.Empty;

        if (xenoname == string.Empty)
            return false;

        foreach (string name in _window._buttons.Keys )
        {
            if(xenoname.Contains(name, StringComparison.OrdinalIgnoreCase) && _window._buttons[name]) //fix lesser appearing on t1 because part of its name is "drone"
                if (name.Contains("Drone", StringComparison.OrdinalIgnoreCase))
                {
                    if (!xenoname.Contains("Lesser", StringComparison.OrdinalIgnoreCase))
                        value = true;
                }
                else
                        value = true;
        }
        return value;
    }
}
