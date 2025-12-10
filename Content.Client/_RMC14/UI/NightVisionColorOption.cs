using System.Linq;
using Content.Client._RMC14.NightVision;
using Content.Client.Options.UI;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.NightVision;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.UI;

public sealed partial class NightVisionColorOption : Control
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private OptionNightVisionColor? _option;

    public NightVisionColorOption()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        InitializeDropdown();
    }

    public void RegisterOption(OptionsTabControlRow control)
    {
        InitializeColorOption(control);
    }

    private void InitializeDropdown()
    {
        var boxContainer = Children.FirstOrDefault() as BoxContainer;
        var colorOption = boxContainer?.Children.Skip(1).FirstOrDefault() as OptionButton;

        if (colorOption == null)
            return;

        foreach (NightVisionColor color in Enum.GetValues(typeof(NightVisionColor)))
            colorOption.AddItem(color.ToString(), (int) color);
    }

    private void InitializeColorOption(OptionsTabControlRow control)
    {
        try
        {
            var prefs = _entityManager.System<NightVisionPreferencesSystem>();
            var nightVisionSystem = _entityManager.System<NightVisionSystem>();

            var boxContainer = Children.FirstOrDefault() as BoxContainer;
            var colorOption = boxContainer?.Children.Skip(1).FirstOrDefault() as OptionButton;

            if (colorOption == null)
                return;

            _option = new OptionNightVisionColor(control, _cfg, colorOption, prefs, nightVisionSystem);
            control.AddOption(_option);
        }
        catch
        {
            // System not ready yet, try again on next frame
            Timer.Spawn(0, () => InitializeColorOption(control));
        }
    }

    private sealed class OptionNightVisionColor : BaseOption
    {
        private readonly IConfigurationManager _cfg;
        private readonly OptionButton _button;
        private readonly NightVisionPreferencesSystem _prefs;
        private readonly NightVisionSystem _nightVisionSystem;

        private NightVisionColor _selectedColor;

        public OptionNightVisionColor(
            OptionsTabControlRow controller,
            IConfigurationManager cfg,
            OptionButton button,
            NightVisionPreferencesSystem prefs,
            NightVisionSystem nightVisionSystem) : base(controller)
        {
            _cfg = cfg;
            _button = button;
            _prefs = prefs;
            _nightVisionSystem = nightVisionSystem;

            _button.OnItemSelected += OnColorSelected;
        }

        public override void LoadValue()
        {
            _selectedColor = ReadConfigColor();
            _button.SelectId((int) _selectedColor);
        }

        public override void SaveValue()
        {
            _cfg.SetCVar(RMCCVars.RMCNightVisionColor, _selectedColor.ToString());
            _prefs.ApplyPreferredColor();
            _nightVisionSystem.RefreshNightVisionColor();
        }

        public override void ResetToDefault()
        {
            _selectedColor = ParseColor(RMCCVars.RMCNightVisionColor.DefaultValue);
            _button.SelectId((int) _selectedColor);
        }

        public override bool IsModified()
        {
            return _selectedColor != ReadConfigColor();
        }

        public override bool IsModifiedFromDefault()
        {
            return _selectedColor != ParseColor(RMCCVars.RMCNightVisionColor.DefaultValue);
        }

        private void OnColorSelected(OptionButton.ItemSelectedEventArgs args)
        {
            _selectedColor = (NightVisionColor) args.Id;
            _button.SelectId(args.Id);
            ValueChanged();
        }

        private NightVisionColor ReadConfigColor()
        {
            var stored = _cfg.GetCVar(RMCCVars.RMCNightVisionColor);
            return ParseColor(stored);
        }

        private static NightVisionColor ParseColor(string value)
        {
            return Enum.TryParse(value, out NightVisionColor color)
                ? color
                : NightVisionColor.Green;
        }
    }
}
